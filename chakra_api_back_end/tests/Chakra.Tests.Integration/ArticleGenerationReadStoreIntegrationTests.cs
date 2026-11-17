using Content.Domain.Enums;
using Content.Infrastructure.Data;
using FluentAssertions;
using Npgsql;
using Xunit;

namespace Chakra.Tests.Integration;

[Collection("postgres")]
public sealed class ArticleGenerationReadStoreIntegrationTests : PostgresIntegrationTestBase
{
    public ArticleGenerationReadStoreIntegrationTests(PostgresTestDatabase database)
        : base(database)
    {
    }

    [PostgresFact]
    public async Task SourceLinkExists_ShouldIncludeSoftDeletedArticles()
    {
        await using var connection = new NpgsqlConnection(ConnectionString);
        await connection.OpenAsync();

        await PostgresTestData.InsertCategoryAsync(connection, 6001, "Generation category");
        await PostgresTestData.InsertArticleAsync(
            connection, 8001, "deleted generated article", 6001, (int)ArticleStatus.Published,
            sourceLink: "https://example.com/deleted", isDeleted: true);
        await PostgresTestData.InsertArticleAsync(
            connection, 8002, "active generated article", 6001, (int)ArticleStatus.Published,
            sourceLink: "https://example.com/active");

        var store = new DapperArticleGenerationReadStore(new NpgsqlConnectionFactory(ConnectionString));

        (await store.SourceLinkExistsAsync("https://example.com/deleted")).Should().BeTrue();
        (await store.SourceLinkExistsAsync("https://example.com/active")).Should().BeTrue();
        (await store.SourceLinkExistsAsync("https://example.com/missing")).Should().BeFalse();
    }

    [PostgresFact]
    public async Task Options_ShouldExcludeSoftDeletedCategoriesAndSeries()
    {
        await using var connection = new NpgsqlConnection(ConnectionString);
        await connection.OpenAsync();

        await PostgresTestData.InsertCategoryAsync(connection, 6101, "Active category", sort: 2);
        await PostgresTestData.InsertCategoryAsync(connection, 6102, "Deleted category", isDeleted: true, sort: 1);
        await PostgresTestData.InsertSeriesAsync(connection, 7101, "Active series");
        await PostgresTestData.InsertSeriesAsync(connection, 7102, "Deleted series", isDeleted: true);

        var store = new DapperArticleGenerationReadStore(new NpgsqlConnectionFactory(ConnectionString));

        var categories = await store.GetCategoriesAsync(CancellationToken.None);
        var series = await store.GetSeriesAsync(CancellationToken.None);

        categories.Should().ContainSingle();
        categories[0].Id.Should().Be(6101);
        categories[0].Name.Should().Be("Active category");
        categories[0].Description.Should().Be("Active category description");

        series.Should().ContainSingle();
        series[0].Id.Should().Be(7101);
        series[0].Name.Should().Be("Active series");
    }
}
