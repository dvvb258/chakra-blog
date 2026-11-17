using Content.Application.Common;
using Content.Application.Common.Models;
using Dapper;

namespace Content.Infrastructure.Data;

/// <summary>
/// 文章自动生成流程的 Dapper 读模型实现
/// </summary>
public sealed class DapperArticleGenerationReadStore : IArticleGenerationReadStore
{
    private const string SourceLinkExistsSql = """
        SELECT EXISTS (
            SELECT 1
            FROM articles
            WHERE source_link = @SourceLink
        )
        """;

    private const string CategoriesSql = """
        SELECT id, name, description
        FROM categories
        WHERE is_del = false
        ORDER BY sort ASC, id ASC
        """;

    private const string SeriesSql = """
        SELECT id, name, description
        FROM series
        WHERE is_del = false
        ORDER BY created_at ASC, id ASC
        """;

    private readonly IContentDbConnectionFactory _connectionFactory;

    /// <summary>
    /// Initializes a new instance of the <see cref="DapperArticleGenerationReadStore"/> class.
    /// </summary>
    /// <param name="connectionFactory">Content 模块数据库连接工厂</param>
    public DapperArticleGenerationReadStore(IContentDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    /// <inheritdoc />
    public async Task<bool> SourceLinkExistsAsync(string sourceLink, CancellationToken cancellationToken = default)
    {
        using var connection = _connectionFactory.CreateConnection();

        var command = new CommandDefinition(
            SourceLinkExistsSql,
            new { SourceLink = sourceLink },
            cancellationToken: cancellationToken);

        return await connection.QuerySingleAsync<bool>(command);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<ArticleGenerationOption>> GetCategoriesAsync(CancellationToken cancellationToken = default)
    {
        using var connection = _connectionFactory.CreateConnection();

        var command = new CommandDefinition(CategoriesSql, cancellationToken: cancellationToken);
        var rows = await connection.QueryAsync<ArticleGenerationOption>(command);

        return rows.AsList();
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<ArticleGenerationOption>> GetSeriesAsync(CancellationToken cancellationToken = default)
    {
        using var connection = _connectionFactory.CreateConnection();

        var command = new CommandDefinition(SeriesSql, cancellationToken: cancellationToken);
        var rows = await connection.QueryAsync<ArticleGenerationOption>(command);

        return rows.AsList();
    }
}
