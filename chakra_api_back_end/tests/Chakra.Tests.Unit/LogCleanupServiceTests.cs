using FluentAssertions;
using Logging.Domain.Entities;
using Logging.Infrastructure.Data;
using Logging.Infrastructure.Services;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using SharedKernel.Domain;
using Xunit;

namespace Chakra.Tests.Unit;

public class LogCleanupServiceTests
{
    private sealed class FakePublisher : IPublisher
    {
        public Task Publish(object notification, CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task Publish<TNotification>(TNotification notification, CancellationToken cancellationToken = default)
            where TNotification : INotification => Task.CompletedTask;
    }

    private static LoggingDbContext CreateInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<LoggingDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        return new LoggingDbContext(options, new FakePublisher());
    }

    private static void SetCreatedAt(Entity entity, DateTime createdAt)
    {
        typeof(Entity).GetProperty(nameof(Entity.CreatedAt))?.SetValue(entity, createdAt);
    }

    [Fact]
    public async Task CleanupExpiredLogsAsync_ShouldDeleteLogsOlderThanCutoff_AndKeepRecentLogs()
    {
        // Arrange
        await using var db = CreateInMemoryDbContext();

        var oldAudit = new AuditLog(1, "admin", "Article", "Delete", "DELETE", "/api/articles/1", null, "127.0.0.1", 20, true);
        SetCreatedAt(oldAudit, DateTime.UtcNow.AddDays(-70));

        var recentAudit = new AuditLog(1, "admin", "Article", "Create", "POST", "/api/articles", null, "127.0.0.1", 35, true);
        SetCreatedAt(recentAudit, DateTime.UtcNow.AddDays(-5));

        var oldException = new ExceptionLog("System.Exception", "Old error", "stack", "/api/fail", "GET", null, null, "127.0.0.1", null, null);
        SetCreatedAt(oldException, DateTime.UtcNow.AddDays(-70));

        var recentException = new ExceptionLog("System.Exception", "Recent error", "stack", "/api/fail2", "GET", null, null, "127.0.0.1", null, null);
        SetCreatedAt(recentException, DateTime.UtcNow.AddDays(-5));

        db.AuditLogs.AddRange(oldAudit, recentAudit);
        db.ExceptionLogs.AddRange(oldException, recentException);
        await db.SaveChangesAsync();

        var service = new LogCleanupService(db, NullLogger<LogCleanupService>.Instance);

        // Act: 清理保留期为 60 天的过期日志
        var (deletedAudits, deletedExceptions) = await service.CleanupExpiredLogsAsync(60);

        // Assert
        deletedAudits.Should().Be(1);
        deletedExceptions.Should().Be(1);

        var remainingAudits = await db.AuditLogs.ToListAsync();
        remainingAudits.Should().ContainSingle();
        remainingAudits[0].Id.Should().Be(recentAudit.Id);

        var remainingExceptions = await db.ExceptionLogs.ToListAsync();
        remainingExceptions.Should().ContainSingle();
        remainingExceptions[0].Id.Should().Be(recentException.Id);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-10)]
    public async Task CleanupExpiredLogsAsync_ShouldDoNothing_WhenRetentionDaysIsZeroOrNegative(int retentionDays)
    {
        // Arrange
        await using var db = CreateInMemoryDbContext();

        var oldAudit = new AuditLog(1, "admin", "Article", "Delete", "DELETE", "/api/articles/1", null, "127.0.0.1", 20, true);
        SetCreatedAt(oldAudit, DateTime.UtcNow.AddDays(-100));
        db.AuditLogs.Add(oldAudit);
        await db.SaveChangesAsync();

        var service = new LogCleanupService(db, NullLogger<LogCleanupService>.Instance);

        // Act
        var (deletedAudits, deletedExceptions) = await service.CleanupExpiredLogsAsync(retentionDays);

        // Assert
        deletedAudits.Should().Be(0);
        deletedExceptions.Should().Be(0);

        var count = await db.AuditLogs.CountAsync();
        count.Should().Be(1);
    }
}
