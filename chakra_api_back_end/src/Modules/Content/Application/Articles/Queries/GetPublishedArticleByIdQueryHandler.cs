using Content.Application.Articles.DTOs;
using Content.Application.Common;
using Content.Domain.Enums;
using Dapper;
using SharedKernel.Application.CQRS;

namespace Content.Application.Articles.Queries;

/// <summary>
/// 根据ID获取已发布文章查询处理器
/// </summary>
public sealed class GetPublishedArticleByIdQueryHandler : IQueryHandler<GetPublishedArticleByIdQuery, ArticleDto?>
{
    private readonly IContentDbConnectionFactory _connectionFactory;

    /// <summary>
    /// Initializes a new instance of the <see cref="GetPublishedArticleByIdQueryHandler"/> class.
    /// </summary>
    /// <param name="connectionFactory">数据库连接工厂</param>
    public GetPublishedArticleByIdQueryHandler(IContentDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    /// <inheritdoc />
    public async Task<ArticleDto?> Handle(GetPublishedArticleByIdQuery request, CancellationToken cancellationToken)
    {
        using var connection = _connectionFactory.CreateConnection();

        const string sql = """
            SELECT a.id, a.title, a.content, a.summary, a.tags,
                   a.category_id,
                   c.name AS category_name,
                   a.author, a.reading_time, a.cover_color, a.cover_url, a.views, a.likes, a.series_id, a.is_pinned, a.created_at, a.updated_at, a.version
            FROM articles a
            LEFT JOIN categories c ON a.category_id = c.id
            WHERE a.id = @Id AND a.is_del = false AND a.status = @PublishedStatus
            """;

        var command = new CommandDefinition(sql, new { Id = request.Id, PublishedStatus = (int)ArticleStatus.Published }, cancellationToken: cancellationToken);
        return await connection.QueryFirstOrDefaultAsync<ArticleDto>(command);
    }
}
