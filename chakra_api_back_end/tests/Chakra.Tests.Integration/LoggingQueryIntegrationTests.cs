using FluentAssertions;
using Logging.Application.Logs.Queries;
using Logging.Infrastructure.Data;
using Npgsql;
using Xunit;

namespace Chakra.Tests.Integration;

[Collection("postgres")]
public sealed class LoggingQueryIntegrationTests : PostgresIntegrationTestBase
{
    public LoggingQueryIntegrationTests(PostgresTestDatabase database)
        : base(database)
    {
    }

    [PostgresFact]
    public async Task AuditLogQuery_ShouldApplyFiltersAndExcludeSoftDeletedRows()
    {
        await using var connection = new NpgsqlConnection(ConnectionString);
        await connection.OpenAsync();

        var now = DateTime.UtcNow;
        await PostgresTestData.InsertAuditLogAsync(connection, 4001, "auth", "admin", true, now.AddMinutes(-3));
        await PostgresTestData.InsertAuditLogAsync(connection, 4002, "content", "editor", false, now.AddMinutes(-2));
        await PostgresTestData.InsertAuditLogAsync(connection, 4003, "auth", "admin", true, now.AddMinutes(-1), isDeleted: true);

        var handler = new GetAuditLogsQueryHandler(new LoggingDbConnectionFactory(ConnectionString));
        var result = await handler.Handle(
            new GetAuditLogsQuery { Module = "auth", IsSuccess = true, Username = "adm" },
            CancellationToken.None);

        result.Total.Should().Be(1);
        result.Items.Should().ContainSingle();
        result.Items[0].Id.Should().Be(4001);
        result.Items[0].Module.Should().Be("auth");
        result.Items[0].DurationMs.Should().Be(12);
        result.Items[0].IsSuccess.Should().BeTrue();
    }

    [PostgresFact]
    public async Task ExceptionLogQueries_ShouldMapDetailAndApplyFilters()
    {
        await using var connection = new NpgsqlConnection(ConnectionString);
        await connection.OpenAsync();

        var now = DateTime.UtcNow;
        await PostgresTestData.InsertExceptionLogAsync(
            connection, 5001, "InvalidOperationException", "boom happened", "/api/x", now.AddMinutes(-2));
        await PostgresTestData.InsertExceptionLogAsync(
            connection, 5002, "InvalidOperationException", "boom deleted", "/api/deleted", now.AddMinutes(-1), isDeleted: true);
        await PostgresTestData.InsertExceptionLogAsync(
            connection, 5003, "ArgumentException", "other error", "/api/y", now);

        var factory = new LoggingDbConnectionFactory(ConnectionString);
        var listHandler = new GetExceptionLogsQueryHandler(factory);
        var listResult = await listHandler.Handle(
            new GetExceptionLogsQuery { Keyword = "boom" },
            CancellationToken.None);

        listResult.Total.Should().Be(1);
        listResult.Items.Should().ContainSingle();
        listResult.Items[0].Id.Should().Be(5001);
        listResult.Items[0].StackTrace.Should().Be("stack trace");
        listResult.Items[0].TraceId.Should().Be("trace-5001");

        var byIdHandler = new GetExceptionLogByIdQueryHandler(factory);
        var detail = await byIdHandler.Handle(new GetExceptionLogByIdQuery(5001), CancellationToken.None);
        var deletedDetail = await byIdHandler.Handle(new GetExceptionLogByIdQuery(5002), CancellationToken.None);

        detail.Should().NotBeNull();
        detail!.Path.Should().Be("/api/x");
        deletedDetail.Should().BeNull();
    }
}
