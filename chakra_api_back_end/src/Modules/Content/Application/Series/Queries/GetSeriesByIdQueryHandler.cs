using Content.Application.Articles.DTOs;
using Content.Application.Common;
using Content.Application.Series.DTOs;
using Dapper;
using SharedKernel.Application.CQRS;

namespace Content.Application.Series.Queries;

/// <summary>
/// 根据ID获取系列查询处理器
/// </summary>
public sealed class GetSeriesByIdQueryHandler : IQueryHandler<GetSeriesByIdQuery, SeriesDetailDto?>
{
    private readonly IContentDbConnectionFactory _connectionFactory;

    /// <summary>
    /// Initializes a new instance of the <see cref="GetSeriesByIdQueryHandler"/> class.
    /// </summary>
    /// <param name="connectionFactory">数据库连接工厂</param>
    public GetSeriesByIdQueryHandler(IContentDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    /// <inheritdoc />
    public async Task<SeriesDetailDto?> Handle(GetSeriesByIdQuery request, CancellationToken cancellationToken)
    {
        using var connection = _connectionFactory.CreateConnection();

        const string seriesSql = """
            SELECT id, name, description, cover_url, theme, author, created_at, updated_at, version
            FROM series
            WHERE id = @Id AND is_del = false
            """;

        var seriesCommand = new CommandDefinition(seriesSql, new { Id = request.Id }, cancellationToken: cancellationToken);
        var series = await connection.QueryFirstOrDefaultAsync<SeriesDetailDto>(seriesCommand);

        if (series is null)
            return null;

        const string articlesSql = """
            SELECT a.id, a.title, a.summary, a.tags,
                   a.category_id,
                   c.name AS category_name,
                   a.author, a.reading_time, a.cover_color, a.cover_url, a.views, a.likes, a.series_id, a.created_at, a.updated_at, a.version
            FROM articles a
            LEFT JOIN categories c ON a.category_id = c.id
            WHERE a.series_id = @SeriesId AND a.is_del = false AND a.status = 1
            ORDER BY a.created_at ASC, a.id ASC
            """;

        var articlesCommand = new CommandDefinition(articlesSql, new { SeriesId = request.Id }, cancellationToken: cancellationToken);
        var articles = (await connection.QueryAsync<ArticleListItemDto>(articlesCommand)).ToList();

        return series with { Articles = articles };
    }
}
