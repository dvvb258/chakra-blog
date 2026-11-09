using System.Text.Json;
using Content.Application.Common;
using Content.Domain.Entities;
using Dapper;
using Npgsql;
using SharedKernel.Application.CQRS;

namespace Content.Application.Settings.Queries;

/// <summary>
/// 获取站点设置覆盖项查询处理器
/// </summary>
public sealed class GetSiteSettingsQueryHandler : IQueryHandler<GetSiteSettingsQuery, JsonElement>
{
    private readonly IContentDbConnectionFactory _connectionFactory;

    /// <summary>
    /// 初始化查询处理器
    /// </summary>
    /// <param name="connectionFactory">数据库连接工厂</param>
    public GetSiteSettingsQueryHandler(IContentDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    /// <inheritdoc />
    public async Task<JsonElement> Handle(GetSiteSettingsQuery request, CancellationToken cancellationToken)
    {
        using var connection = _connectionFactory.CreateConnection();

        const string sql = """
            SELECT config::text AS ConfigJson
            FROM site_settings
            WHERE id = @Id AND is_del = false
            LIMIT 1
            """;

        try
        {
            var json = await connection.QueryFirstOrDefaultAsync<string>(
                new CommandDefinition(sql, new { Id = SiteSetting.SingletonId }, cancellationToken: cancellationToken));

            return ParseConfig(json);
        }
        catch (PostgresException ex) when (ex.SqlState == PostgresErrorCodes.UndefinedTable)
        {
            // 老部署尚未执行升级脚本：公开读取按“无覆盖项”处理，保证展示端仍可用
            return EmptyObject();
        }
    }

    /// <summary>
    /// 解析配置 JSON，异常数据统一回退为空对象，避免单条脏数据导致展示端整页失败
    /// </summary>
    /// <param name="json">数据库中的原始 JSON 文本</param>
    /// <returns>配置 JSON 元素</returns>
    private static JsonElement ParseConfig(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return EmptyObject();
        }

        try
        {
            using var document = JsonDocument.Parse(json);
            return document.RootElement.ValueKind == JsonValueKind.Object
                ? document.RootElement.Clone()
                : EmptyObject();
        }
        catch (JsonException)
        {
            return EmptyObject();
        }
    }

    private static JsonElement EmptyObject()
    {
        using var document = JsonDocument.Parse("{}");
        return document.RootElement.Clone();
    }
}
