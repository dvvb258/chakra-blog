using Content.Application.Articles.Queries;
using Content.Application.Common;
using Content.Application.Dashboard.Queries;
using Content.Application.Series.Queries;
using Content.Domain.Enums;
using Content.Infrastructure.Data;
using FluentAssertions;
using Npgsql;
using Xunit;

namespace Chakra.Tests.Integration;

[Collection("postgres")]
public sealed class ContentArticleQueryIntegrationTests : PostgresIntegrationTestBase
{
    public ContentArticleQueryIntegrationTests(PostgresTestDatabase database)
        : base(database)
    {
    }

    [PostgresFact]
    public async Task PublishedArticleList_ShouldApplyTagSoftDeleteAndPaging()
    {
        await using var connection = new NpgsqlConnection(ConnectionString);
        await connection.OpenAsync();

        await PostgresTestData.InsertCategoryAsync(connection, 10, "C#");
        await PostgresTestData.InsertSeriesAsync(connection, 20, ".NET");

        var now = DateTime.UtcNow;
        await PostgresTestData.InsertArticleAsync(
            connection, 1001, "first", 10, (int)ArticleStatus.Published,
            tags: ["dotnet", "csharp"], seriesId: 20, views: 100, createdAt: now.AddMinutes(-3));
        await PostgresTestData.InsertArticleAsync(
            connection, 1002, "second", 10, (int)ArticleStatus.Published,
            tags: ["ai"], views: 200, createdAt: now.AddMinutes(-2));
        await PostgresTestData.InsertArticleAsync(
            connection, 1003, "draft", 10, (int)ArticleStatus.Draft,
            tags: ["dotnet"], views: 300, createdAt: now.AddMinutes(-1));
        await PostgresTestData.InsertArticleAsync(
            connection, 1004, "deleted", 10, (int)ArticleStatus.Published,
            tags: ["dotnet"], views: 400, isDeleted: true, createdAt: now);

        var handler = new GetPublishedArticlesQueryHandler(new NpgsqlConnectionFactory(ConnectionString));
        var result = await handler.Handle(
            new GetPublishedArticlesQuery { Tag = "dotnet", PageSize = 1 },
            CancellationToken.None);

        result.Total.Should().Be(1);
        result.Items.Should().ContainSingle();
        result.Items[0].Id.Should().Be(1001);
        result.Items[0].Tags.Should().BeEquivalentTo("dotnet", "csharp");
        result.Items[0].CategoryName.Should().Be("C#");
        result.Items[0].SeriesId.Should().Be(20);
        result.Items[0].Views.Should().Be(100);
    }

    [PostgresFact]
    public async Task AdminArticleList_ShouldApplyStatusTagAndKeywordFilters()
    {
        await using var connection = new NpgsqlConnection(ConnectionString);
        await connection.OpenAsync();

        await PostgresTestData.InsertCategoryAsync(connection, 11, "Architecture");
        await PostgresTestData.InsertArticleAsync(
            connection, 2001, "matching article", 11, (int)ArticleStatus.Published, tags: ["dotnet"]);
        await PostgresTestData.InsertArticleAsync(
            connection, 2002, "matching draft", 11, (int)ArticleStatus.Draft, tags: ["dotnet"]);
        await PostgresTestData.InsertArticleAsync(
            connection, 2003, "matching article", 11, (int)ArticleStatus.Published, tags: ["ai"]);
        await PostgresTestData.InsertArticleAsync(
            connection, 2004, "deleted article", 11, (int)ArticleStatus.Published,
            tags: ["dotnet"], isDeleted: true);

        var handler = new GetAdminArticlesQueryHandler(new NpgsqlConnectionFactory(ConnectionString));
        var result = await handler.Handle(
            new GetAdminArticlesQuery
            {
                Keyword = "matching",
                Status = (int)ArticleStatus.Published,
                Tag = "dotnet",
            },
            CancellationToken.None);

        result.Total.Should().Be(1);
        result.Items.Should().ContainSingle();
        result.Items[0].Id.Should().Be(2001);
        result.Items[0].CategoryName.Should().Be("Architecture");
    }

    [PostgresFact]
    public async Task SeriesDetail_ShouldReturnOnlyPublishedArticles()
    {
        await using var connection = new NpgsqlConnection(ConnectionString);
        await connection.OpenAsync();

        await PostgresTestData.InsertCategoryAsync(connection, 12, "Series category");
        await PostgresTestData.InsertSeriesAsync(connection, 21, "Series");
        await PostgresTestData.InsertArticleAsync(
            connection, 3001, "published", 12, (int)ArticleStatus.Published, seriesId: 21);
        await PostgresTestData.InsertArticleAsync(
            connection, 3002, "draft", 12, (int)ArticleStatus.Draft, seriesId: 21);

        var handler = new GetSeriesByIdQueryHandler(new NpgsqlConnectionFactory(ConnectionString));
        var result = await handler.Handle(new GetSeriesByIdQuery(21), CancellationToken.None);

        result.Should().NotBeNull();
        result!.Articles.Should().ContainSingle();
        result.Articles[0].Id.Should().Be(3001);
        result.Articles[0].CategoryName.Should().Be("Series category");
    }

    [PostgresFact]
    public async Task DashboardSummary_ShouldMapAggregateValuesAndTrend()
    {
        await using var connection = new NpgsqlConnection(ConnectionString);
        await connection.OpenAsync();

        await PostgresTestData.InsertCategoryAsync(connection, 13, "Dashboard category");
        await PostgresTestData.InsertSeriesAsync(connection, 22, "Dashboard series");

        var now = DateTime.UtcNow;
        await PostgresTestData.InsertArticleAsync(
            connection, 4001, "dashboard one", 13, (int)ArticleStatus.Published,
            seriesId: 22, views: 10, createdAt: now.AddMinutes(-2));
        await PostgresTestData.InsertArticleAsync(
            connection, 4002, "dashboard two", 13, (int)ArticleStatus.Published,
            seriesId: 22, views: 20, createdAt: now.AddMinutes(-1));
        await PostgresTestData.InsertArticleAsync(
            connection, 4003, "dashboard deleted", 13, (int)ArticleStatus.Published,
            seriesId: 22, views: 99, isDeleted: true, createdAt: now);

        var handler = new GetDashboardSummaryQueryHandler(
            new NpgsqlConnectionFactory(ConnectionString),
            new FakeStatsTracker());
        var result = await handler.Handle(new GetDashboardSummaryQuery(), CancellationToken.None);

        result.TotalArticles.Should().Be(2);
        result.TotalViews.Should().Be(30);
        result.TotalSeries.Should().Be(1);
        result.TotalVisits.Should().Be(123);
        result.CategoryDistribution.Should().ContainSingle();
        result.CategoryDistribution[0].Category.Should().Be("Dashboard category");
        result.CategoryDistribution[0].Count.Should().Be(2);
        result.RecentArticles.Should().HaveCount(2);
        result.TrendData.Should().HaveCount(7);
        result.TrendData.Sum(x => x.ArticleCount).Should().Be(2);
    }

    private sealed class FakeStatsTracker : ISystemStatsTracker
    {
        public Task IncrementSystemVisitsAsync(CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }

        public Task<long> GetSystemVisitsAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(123L);
        }
    }
}
