using Content.Application.Articles.DTOs;
using Content.Application.Common;
using Dapper;
using SharedKernel.Application;
using SharedKernel.Application.CQRS;

namespace Content.Application.Articles.Queries;

/// <summary>
/// 管理端文章分页列表查询处理器
/// </summary>
public sealed class GetAdminArticlesQueryHandler : IQueryHandler<GetAdminArticlesQuery, PagedResult<ArticleListItemDto>>
{
    private readonly IContentDbConnectionFactory _connectionFactory;

    /// <summary>
    /// Initializes a new instance of the <see cref="GetAdminArticlesQueryHandler"/> class.
    /// </summary>
    /// <param name="connectionFactory">数据库连接工厂</param>
    public GetAdminArticlesQueryHandler(IContentDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    /// <inheritdoc />
    public async Task<PagedResult<ArticleListItemDto>> Handle(GetAdminArticlesQuery request, CancellationToken cancellationToken)
    {
        using var connection = _connectionFactory.CreateConnection();

        var conditions = new List<string> { "a.is_del = false" };

        if (!string.IsNullOrWhiteSpace(request.Keyword))
        {
            conditions.Add("(a.title ILIKE @Keyword OR a.summary ILIKE @Keyword)");
        }

        if (request.CategoryId.HasValue && request.CategoryId.Value > 0)
        {
            conditions.Add("a.category_id = @CategoryId");
        }

        if (!string.IsNullOrWhiteSpace(request.Tag))
        {
            conditions.Add("@Tag = ANY(a.tags)");
        }

        if (request.SeriesId.HasValue && request.SeriesId.Value > 0)
        {
            conditions.Add("a.series_id = @SeriesId");
        }

        if (request.Status.HasValue)
        {
            conditions.Add("a.status = @Status");
        }

        if (request.IsPinned.HasValue)
        {
            conditions.Add("a.is_pinned = @IsPinned");
        }

        if (request.CreatedByUserId.HasValue && request.CreatedByUserId.Value > 0)
        {
            conditions.Add("a.created_by_user_id = @CreatedByUserId");
        }

        var whereClause = "WHERE " + string.Join(" AND ", conditions);

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
                   a.author, a.reading_time, a.cover_color, a.cover_url, a.views, a.likes, a.series_id, a.is_pinned, a.status, a.created_by_user_id, a.created_at, a.updated_at, a.version
            FROM articles a
            LEFT JOIN categories c ON a.category_id = c.id
            {whereClause}
            ORDER BY a.is_pinned DESC, a.created_at DESC, a.id DESC
            LIMIT @PageSize OFFSET @Offset
            """;

        var parameters = new
        {
            Keyword = $"%{request.Keyword}%",
            CategoryId = request.CategoryId,
            Tag = request.Tag,
            SeriesId = request.SeriesId,
            Status = request.Status,
            IsPinned = request.IsPinned,
            CreatedByUserId = request.CreatedByUserId,
            Offset = (request.PageIndex - 1) * request.PageSize,
            PageSize = request.PageSize,
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
            PageSize = request.PageSize,
        };
    }
}
