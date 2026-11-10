using Content.Application.Articles.DTOs;
using Content.Application.Common;
using Dapper;
using SharedKernel.Application.CQRS;

namespace Content.Application.Articles.Queries;

/// <summary>
/// 管理端文章详情查询处理器
/// </summary>
public sealed class GetAdminArticleByIdQueryHandler : IQueryHandler<GetAdminArticleByIdQuery, ArticleDto?>
{
    private readonly IContentDbConnectionFactory _connectionFactory;

    /// <summary>
    /// Initializes a new instance of the <see cref="GetAdminArticleByIdQueryHandler"/> class.
    /// </summary>
    /// <param name="connectionFactory">数据库连接工厂</param>
    public GetAdminArticleByIdQueryHandler(IContentDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    /// <inheritdoc />
    public async Task<ArticleDto?> Handle(GetAdminArticleByIdQuery request, CancellationToken cancellationToken)
    {
        using var connection = _connectionFactory.CreateConnection();

        const string sql = """
            SELECT a.id, a.title, a.content, a.summary, a.tags,
                   a.category_id,
                   c.name AS category_name,
                   a.author, a.reading_time, a.cover_color, a.cover_url, a.views, a.likes, a.series_id, a.is_pinned, a.status, a.created_by_user_id, a.created_at, a.updated_at, a.version
            FROM articles a
            LEFT JOIN categories c ON a.category_id = c.id
            WHERE a.id = @Id AND a.is_del = false
            """;

        var command = new CommandDefinition(sql, new { Id = request.Id }, cancellationToken: cancellationToken);
        return await connection.QueryFirstOrDefaultAsync<ArticleDto>(command);
    }
}
