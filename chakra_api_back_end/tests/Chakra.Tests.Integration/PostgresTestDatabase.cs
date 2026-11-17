using Dapper;
using Npgsql;
using Xunit;

namespace Chakra.Tests.Integration;

public sealed class PostgresTestDatabase : IAsyncLifetime
{
    private const string ResetSql = """
        TRUNCATE TABLE
            articles,
            categories,
            series,
            users,
            refresh_tokens,
            audit_logs,
            exception_logs,
            role_menus,
            roles,
            menus,
            site_settings,
            system_stats
        RESTART IDENTITY CASCADE;
        """;

    private string? _adminConnectionString;
    private string? _databaseName;

    public string ConnectionString { get; private set; } = string.Empty;

    public async Task InitializeAsync()
    {
        var rawConnectionString = Environment.GetEnvironmentVariable(PostgresFactAttribute.EnvironmentVariable);
        if (string.IsNullOrWhiteSpace(rawConnectionString))
        {
            return;
        }

        var builder = new NpgsqlConnectionStringBuilder(rawConnectionString);
        _adminConnectionString = builder.ConnectionString;
        _databaseName = $"chakra_it_{Guid.NewGuid():N}";

        await using (var adminConnection = new NpgsqlConnection(_adminConnectionString))
        {
            await adminConnection.OpenAsync();
            await using var createCommand = new NpgsqlCommand($"CREATE DATABASE \"{_databaseName}\";", adminConnection);
            await createCommand.ExecuteNonQueryAsync();
        }

        builder.Database = _databaseName;
        ConnectionString = builder.ConnectionString;

        var scriptPath = Path.Combine(AppContext.BaseDirectory, "scripts", "sql", "init.sql");
        var schemaSql = await File.ReadAllTextAsync(scriptPath);

        await using var connection = new NpgsqlConnection(ConnectionString);
        await connection.OpenAsync();
        await using var schemaCommand = new NpgsqlCommand(schemaSql, connection)
        {
            CommandTimeout = 180,
        };
        await schemaCommand.ExecuteNonQueryAsync();
    }

    public async Task ResetAsync()
    {
        if (string.IsNullOrWhiteSpace(ConnectionString))
        {
            return;
        }

        await using var connection = new NpgsqlConnection(ConnectionString);
        await connection.OpenAsync();
        await connection.ExecuteAsync(ResetSql);
    }

    public async Task DisposeAsync()
    {
        if (string.IsNullOrWhiteSpace(_adminConnectionString) || string.IsNullOrWhiteSpace(_databaseName))
        {
            return;
        }

        NpgsqlConnection.ClearAllPools();

        await using var adminConnection = new NpgsqlConnection(_adminConnectionString);
        await adminConnection.OpenAsync();

        await using (var terminateCommand = new NpgsqlCommand(
            "SELECT pg_terminate_backend(pid) FROM pg_stat_activity WHERE datname = @databaseName AND pid <> pg_backend_pid();",
            adminConnection))
        {
            terminateCommand.Parameters.AddWithValue("databaseName", _databaseName);
            await terminateCommand.ExecuteNonQueryAsync();
        }

        await using var dropCommand = new NpgsqlCommand($"DROP DATABASE IF EXISTS \"{_databaseName}\";", adminConnection);
        await dropCommand.ExecuteNonQueryAsync();
    }
}
