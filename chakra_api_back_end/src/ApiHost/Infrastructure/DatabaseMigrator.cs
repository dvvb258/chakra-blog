using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using Npgsql;

[assembly: InternalsVisibleTo("Chakra.Tests.Unit")]

namespace ApiHost.Infrastructure;

/// <summary>
/// 数据库迁移运行器（在应用启动时自动执行增量 SQL 脚本）
/// </summary>
public static class DatabaseMigrator
{
    private record MigrationScript(string Version, string Description, Func<Task<string>> GetSqlAsync);

    /// <summary>
    /// 执行未应用的数据库迁移脚本
    /// </summary>
    /// <param name="connectionString">数据库连接字符串</param>
    /// <param name="logger">日志记录器</param>
    /// <param name="cancellationToken">取消标记</param>
    public static async Task MigrateAsync(string connectionString, ILogger logger, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            logger.LogWarning("[DatabaseMigrator] 未配置 DefaultConnection 连接字符串，跳过自动数据库迁移。");
            return;
        }

        const int maxRetries = 5;
        for (var attempt = 1; attempt <= maxRetries; attempt++)
        {
            try
            {
                await RunMigrationAsync(connectionString, logger, cancellationToken);
                return;
            }
            catch (NpgsqlException ex) when (attempt < maxRetries)
            {
                logger.LogWarning(ex, "[DatabaseMigrator] 连接数据库进行迁移失败 (第 {Attempt}/{MaxRetries} 次重试)，2 秒后重试...", attempt, maxRetries);
                await Task.Delay(TimeSpan.FromSeconds(2), cancellationToken);
            }
        }
    }

    private static async Task RunMigrationAsync(string connectionString, ILogger logger, CancellationToken cancellationToken)
    {
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);

        // 1. 确保迁移历史记录表存在
        const string createMigrationsTableSql = """
            CREATE TABLE IF NOT EXISTS __schema_migrations (
                version VARCHAR(50) PRIMARY KEY,
                description VARCHAR(200) NOT NULL,
                applied_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
            );
            """;
        await using (var cmd = new NpgsqlCommand(createMigrationsTableSql, connection))
        {
            await cmd.ExecuteNonQueryAsync(cancellationToken);
        }

        // 2. 读取已执行的迁移版本
        var appliedVersions = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        const string queryAppliedSql = "SELECT version FROM __schema_migrations;";
        await using (var cmd = new NpgsqlCommand(queryAppliedSql, connection))
        await using (var reader = await cmd.ExecuteReaderAsync(cancellationToken))
        {
            while (await reader.ReadAsync(cancellationToken))
            {
                appliedVersions.Add(reader.GetString(0));
            }
        }

        // 3. 收集所有迁移脚本并筛选出待执行项
        var allScripts = DiscoverScripts();
        var pendingScripts = allScripts
            .Where(s => !appliedVersions.Contains(s.Version))
            .OrderBy(s => s.Version, StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (pendingScripts.Count == 0)
        {
            logger.LogInformation("[DatabaseMigrator] 数据库结构已是最新版本，无需执行任何增量迁移 (当前已记录 {AppliedCount} 项)。", appliedVersions.Count);
            return;
        }

        logger.LogInformation("[DatabaseMigrator] 检测到 {PendingCount} 个待执行的数据库迁移脚本，开始按序应用...", pendingScripts.Count);

        // 4. 按版本序依次执行待应用的迁移脚本
        foreach (var script in pendingScripts)
        {
            logger.LogInformation("[DatabaseMigrator] 正在执行迁移 [{Version}]: {Description}...", script.Version, script.Description);
            var rawSql = await script.GetSqlAsync();
            var sql = CleanSql(rawSql);

            await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
            try
            {
                if (!string.IsNullOrWhiteSpace(sql))
                {
                    await using var execCmd = new NpgsqlCommand(sql, connection, transaction);
                    execCmd.CommandTimeout = 180; // 3 分钟超时
                    await execCmd.ExecuteNonQueryAsync(cancellationToken);
                }

                const string recordMigrationSql = """
                    INSERT INTO __schema_migrations (version, description, applied_at)
                    VALUES (@version, @description, NOW());
                    """;
                await using (var recordCmd = new NpgsqlCommand(recordMigrationSql, connection, transaction))
                {
                    recordCmd.Parameters.AddWithValue("version", script.Version);
                    recordCmd.Parameters.AddWithValue("description", script.Description);
                    await recordCmd.ExecuteNonQueryAsync(cancellationToken);
                }

                await transaction.CommitAsync(cancellationToken);
                logger.LogInformation("[DatabaseMigrator] 迁移执行成功 [{Version}]: {Description}。", script.Version, script.Description);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync(cancellationToken);
                logger.LogCritical(ex, "[DatabaseMigrator] 迁移执行失败 [{Version}]: {Description}！已自动回滚，应用启动终止。", script.Version, script.Description);
                throw;
            }
        }

        logger.LogInformation("[DatabaseMigrator] 所有待执行迁移脚本应用完成！");
    }

    private static List<MigrationScript> DiscoverScripts()
    {
        var scripts = new Dictionary<string, MigrationScript>(StringComparer.OrdinalIgnoreCase);
        var assembly = typeof(DatabaseMigrator).Assembly;

        // 1. 扫描嵌入程序集的 SQL 迁移文件 (由 ApiHost.csproj 编译期自动嵌入)
        var resourceRegex = new Regex(@"(?:^|\.)(\d{3,})_([^.]+)\.sql$", RegexOptions.IgnoreCase);
        foreach (var resourceName in assembly.GetManifestResourceNames())
        {
            var match = resourceRegex.Match(resourceName);
            if (match.Success)
            {
                var version = match.Groups[1].Value;
                var description = match.Groups[2].Value.Replace('_', ' ');
                var capturedName = resourceName;
                scripts[version] = new MigrationScript(version, description, async () =>
                {
                    await using var stream = assembly.GetManifestResourceStream(capturedName);
                    if (stream == null) return string.Empty;
                    using var reader = new StreamReader(stream, Encoding.UTF8);
                    return await reader.ReadToEndAsync();
                });
            }
        }

        // 2. 本地磁盘目录兜底扫描 (便于本地无重新编译下的热更新或脚本排查)
        var candidatePaths = new[]
        {
            Path.Combine(AppContext.BaseDirectory, "scripts", "sql", "upgrade"),
            Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "scripts", "sql", "upgrade"),
            Path.Combine(Directory.GetCurrentDirectory(), "scripts", "sql", "upgrade"),
            Path.Combine(Directory.GetCurrentDirectory(), "chakra_api_back_end", "scripts", "sql", "upgrade"),
        };

        var fileRegex = new Regex(@"^(\d{3,})_(.+)\.sql$", RegexOptions.IgnoreCase);
        foreach (var dir in candidatePaths)
        {
            if (!Directory.Exists(dir)) continue;

            foreach (var filePath in Directory.GetFiles(dir, "*.sql"))
            {
                var fileName = Path.GetFileName(filePath);
                var match = fileRegex.Match(fileName);
                if (match.Success)
                {
                    var version = match.Groups[1].Value;
                    if (!scripts.ContainsKey(version))
                    {
                        var description = match.Groups[2].Value.Replace('_', ' ');
                        var capturedPath = filePath;
                        scripts[version] = new MigrationScript(version, description, async () =>
                        {
                            return await File.ReadAllTextAsync(capturedPath, Encoding.UTF8);
                        });
                    }
                }
            }
        }

        return scripts.Values.ToList();
    }

    /// <summary>
    /// 清理 SQL 文本中的外部事务声明（BEGIN / COMMIT），由 Migrator 显式管理原子事务
    /// </summary>
    /// <param name="sql">原始 SQL 语句</param>
    /// <returns>清理后的 SQL 语句</returns>
    internal static string CleanSql(string sql)
    {
        var lines = sql.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
        var cleanedLines = new List<string>(lines.Length);

        foreach (var line in lines)
        {
            var trimmed = line.Trim();
            if (Regex.IsMatch(trimmed, @"^(BEGIN|COMMIT)(\s+TRANSACTION)?\s*;", RegexOptions.IgnoreCase))
            {
                continue;
            }

            cleanedLines.Add(line);
        }

        return string.Join(Environment.NewLine, cleanedLines);
    }
}
