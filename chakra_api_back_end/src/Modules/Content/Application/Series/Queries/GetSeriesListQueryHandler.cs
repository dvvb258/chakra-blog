using Content.Application.Common;
using Content.Application.Series.DTOs;
using Dapper;
using SharedKernel.Application;
using SharedKernel.Application.CQRS;

namespace Content.Application.Series.Queries;

/// <summary>
/// 获取系列列表查询处理器
/// </summary>
public sealed class GetSeriesListQueryHandler : IQueryHandler<GetSeriesListQuery, PagedResult<SeriesListItemDto>>
{
    private readonly IContentDbConnectionFactory _connectionFactory;

    /// <summary>
    /// Initializes a new instance of the <see cref="GetSeriesListQueryHandler"/> class.
    /// </summary>
    /// <param name="connectionFactory">数据库连接工厂</param>
    public GetSeriesListQueryHandler(IContentDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    /// <inheritdoc />
    public async Task<PagedResult<SeriesListItemDto>> Handle(GetSeriesListQuery request, CancellationToken cancellationToken)
    {
        using var connection = _connectionFactory.CreateConnection();

        var conditions = new List<string> { "is_del = false" };

        if (!string.IsNullOrWhiteSpace(request.Keyword))
            conditions.Add("name ILIKE @Keyword");

        if (!string.IsNullOrWhiteSpace(request.Author))
            conditions.Add("author = @Author");

        var whereClause = "WHERE " + string.Join(" AND ", conditions);

        var countSql = $"SELECT COUNT(*) FROM series {whereClause}";
        var dataSql = $"""
            SELECT id, name, description, cover_url, theme, author, created_at, updated_at, version,
                   (SELECT COUNT(*) FROM articles WHERE series_id = s.id AND is_del = false) AS article_count
            FROM series s
            {whereClause}
            ORDER BY created_at DESC, id DESC
            LIMIT @PageSize OFFSET @Offset
            """;

        var parameters = new
        {
            Keyword = $"%{request.Keyword}%",
            Author = request.Author,
            Offset = (request.PageIndex - 1) * request.PageSize,
            PageSize = request.PageSize
        };

        var countCommand = new CommandDefinition(countSql, parameters, cancellationToken: cancellationToken);
        var total = await connection.ExecuteScalarAsync<int>(countCommand);

        var dataCommand = new CommandDefinition(dataSql, parameters, cancellationToken: cancellationToken);
        var items = (await connection.QueryAsync<SeriesListItemDto>(dataCommand)).ToList();

        return new PagedResult<SeriesListItemDto>
        {
            Items = items,
            Total = total,
            PageIndex = request.PageIndex,
            PageSize = request.PageSize
        };
    }
}
