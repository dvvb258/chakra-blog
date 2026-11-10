using Content.Application.Articles.DTOs;
using Content.Application.Common;
using Content.Domain.Enums;
using Dapper;
using SharedKernel.Application;
using SharedKernel.Application.CQRS;

namespace Content.Application.Articles.Queries;

/// <summary>
/// 获取已发布文章列表查询处理器
/// </summary>
public sealed class GetPublishedArticlesQueryHandler : IQueryHandler<GetPublishedArticlesQuery, PagedResult<ArticleListItemDto>>
{
    private readonly IContentDbConnectionFactory _connectionFactory;

    /// <summary>
    /// Initializes a new instance of the <see cref="GetPublishedArticlesQueryHandler"/> class.
    /// </summary>
    /// <param name="connectionFactory">数据库连接工厂</param>
    public GetPublishedArticlesQueryHandler(IContentDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    /// <inheritdoc />
    public async Task<PagedResult<ArticleListItemDto>> Handle(GetPublishedArticlesQuery request, CancellationToken cancellationToken)
    {
        using var connection = _connectionFactory.CreateConnection();

        var conditions = new List<string> { "a.is_del = false", "a.status = @PublishedStatus" };

        if (!string.IsNullOrWhiteSpace(request.Keyword))
            conditions.Add("a.title ILIKE @Keyword");

        if (request.CategoryId.HasValue && request.CategoryId.Value > 0)
            conditions.Add("a.category_id = @CategoryId");

        if (!string.IsNullOrWhiteSpace(request.Author))
            conditions.Add("a.author = @Author");

        if (!string.IsNullOrWhiteSpace(request.Tag))
            conditions.Add("@Tag = ANY(a.tags)");

        var whereClause = "WHERE " + string.Join(" AND ", conditions);

        var orderClause = request.SortBy?.Trim().ToLowerInvariant() switch
        {
            "views" => "ORDER BY a.is_pinned DESC, a.views DESC, a.created_at DESC",
            "likes" => "ORDER BY a.is_pinned DESC, a.likes DESC, a.created_at DESC",
            _ => "ORDER BY a.is_pinned DESC, a.created_at DESC, a.id DESC"
        };

        var countSql = $"""
            SELECT COUNT(*) 
            FROM articles a
            LEFT JOIN categories c ON a.category_id = c.id
            {whereClause}
            """;
        var dataSql = $"""
            SELECT a.id, a.title, a.summary, a.tags,
                   a.category_id,
                   c.name AS category_name,
                   a.author, a.reading_time, a.cover_color, a.cover_url, a.views, a.likes, a.series_id, a.is_pinned, a.created_at, a.updated_at, a.version
            FROM articles a
            LEFT JOIN categories c ON a.category_id = c.id
            {whereClause}
            {orderClause}
            LIMIT @PageSize OFFSET @Offset
            """;

        var parameters = new
        {
            PublishedStatus = (int)ArticleStatus.Published,
            Keyword = $"%{request.Keyword}%",
            CategoryId = request.CategoryId,
            Author = request.Author,
            Tag = request.Tag,
            Offset = (request.PageIndex - 1) * request.PageSize,
            PageSize = request.PageSize
        };

        var countCommand = new CommandDefinition(countSql, parameters, cancellationToken: cancellationToken);
        var total = await connection.QueryFirstOrDefaultAsync<int>(countCommand);

        var dataCommand = new CommandDefinition(dataSql, parameters, cancellationToken: cancellationToken);
        var items = (await connection.QueryAsync<ArticleListItemDto>(dataCommand)).ToList();

        return new PagedResult<ArticleListItemDto>
        {
            Items = items,
            Total = total,
            PageIndex = request.PageIndex,
            PageSize = request.PageSize
        };
    }
}
