using Content.Application.Articles.DTOs;
using Content.Application.Common;
using Content.Domain.Enums;
using Dapper;
using SharedKernel.Application.CQRS;

namespace Content.Application.Articles.Queries;

/// <summary>
/// 获取相邻文章查询处理器
/// </summary>
public sealed class GetArticleNeighborsQueryHandler : IQueryHandler<GetArticleNeighborsQuery, ArticleNeighborsDto?>
{
    private readonly IContentDbConnectionFactory _connectionFactory;

    /// <summary>
    /// Initializes a new instance of the <see cref="GetArticleNeighborsQueryHandler"/> class.
    /// </summary>
    /// <param name="connectionFactory">数据库连接工厂</param>
    public GetArticleNeighborsQueryHandler(IContentDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    /// <inheritdoc />
    public async Task<ArticleNeighborsDto?> Handle(GetArticleNeighborsQuery request, CancellationToken cancellationToken)
    {
        using var connection = _connectionFactory.CreateConnection();

        // 1. 确认当前文章存在，并获取其创建时间
        const string currentSql = """
            SELECT created_at
            FROM articles
            WHERE id = @Id AND is_del = false AND status = @PublishedStatus
            """;

        var currentCreatedAt = await connection.QueryFirstOrDefaultAsync<DateTime?>(
            new CommandDefinition(currentSql, new { Id = request.Id, PublishedStatus = (int)ArticleStatus.Published }, cancellationToken: cancellationToken));

        if (currentCreatedAt == null)
        {
            return null;
        }

        // 2. 使用单条 SQL (UNION ALL) 一次性查出上一篇与下一篇，避免在同一数据库连接上并发执行多条命令
        const string neighborsSql = """
            (
                SELECT id AS Id, title AS Title, 'prev' AS Direction
                FROM articles
                WHERE is_del = false
                  AND status = @PublishedStatus
                  AND (created_at < @CreatedAt OR (created_at = @CreatedAt AND id < @Id))
                ORDER BY created_at DESC, id DESC
                LIMIT 1
            )
            UNION ALL
            (
                SELECT id AS Id, title AS Title, 'next' AS Direction
                FROM articles
                WHERE is_del = false
                  AND status = @PublishedStatus
                  AND (created_at > @CreatedAt OR (created_at = @CreatedAt AND id > @Id))
                ORDER BY created_at ASC, id ASC
                LIMIT 1
            )
            """;

        var neighbors = await connection.QueryAsync<NeighborRow>(
            new CommandDefinition(neighborsSql, new { Id = request.Id, CreatedAt = currentCreatedAt.Value, PublishedStatus = (int)ArticleStatus.Published }, cancellationToken: cancellationToken));

        ArticleNeighborItemDto? prev = null;
        ArticleNeighborItemDto? next = null;

        foreach (var item in neighbors)
        {
            if (item.Direction == "prev")
            {
                prev = new ArticleNeighborItemDto { Id = item.Id, Title = item.Title };
            }
            else if (item.Direction == "next")
            {
                next = new ArticleNeighborItemDto { Id = item.Id, Title = item.Title };
            }
        }

        return new ArticleNeighborsDto
        {
            Prev = prev,
            Next = next,
        };
    }

    private sealed record NeighborRow(long Id, string Title, string Direction);
}
