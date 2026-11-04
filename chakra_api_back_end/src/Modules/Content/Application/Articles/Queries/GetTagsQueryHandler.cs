using Content.Application.Common;
using Dapper;
using SharedKernel.Application.CQRS;

namespace Content.Application.Articles.Queries;

/// <summary>
/// 获取全量文章热门标签列表查询处理器
/// </summary>
public sealed class GetTagsQueryHandler : IQueryHandler<GetTagsQuery, List<string>>
{
    private readonly IContentDbConnectionFactory _connectionFactory;

    public GetTagsQueryHandler(IContentDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<List<string>> Handle(GetTagsQuery request, CancellationToken cancellationToken)
    {
        using var connection = _connectionFactory.CreateConnection();

        const string sql = """
            SELECT tag
            FROM (
                SELECT UNNEST(tags) AS tag 
                FROM articles 
                WHERE is_del = false AND status = 1
            ) t
            WHERE tag IS NOT NULL AND BTRIM(tag) <> ''
            GROUP BY tag
            ORDER BY COUNT(*) DESC, tag ASC
            LIMIT @Limit;
            """;

        var command = new CommandDefinition(
            sql,
            new { Limit = Math.Clamp(request.Limit, 1, 100) },
            cancellationToken: cancellationToken);

        var tags = await connection.QueryAsync<string>(command);
        return tags.ToList();
    }
}
