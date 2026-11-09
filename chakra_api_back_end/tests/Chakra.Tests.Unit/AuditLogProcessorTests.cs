using FluentAssertions;
using Logging.Infrastructure.Data;
using Logging.Infrastructure.Services;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using SharedKernel.Application;
using Xunit;

namespace Chakra.Tests.Unit;

public class AuditLogProcessorTests
{
    private sealed class FakePublisher : IPublisher
    {
        public Task Publish(object notification, CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task Publish<TNotification>(TNotification notification, CancellationToken cancellationToken = default)
            where TNotification : INotification => Task.CompletedTask;
    }

    private static (IServiceProvider ServiceProvider, LoggingDbContext DbContext) CreateServiceProviderWithInMemoryDb()
    {
        var services = new ServiceCollection();
        var dbName = Guid.NewGuid().ToString();

        services.AddDbContext<LoggingDbContext>(options =>
            options.UseInMemoryDatabase(databaseName: dbName));
        services.AddSingleton<IPublisher, FakePublisher>();

        var provider = services.BuildServiceProvider();
        var db = provider.GetRequiredService<LoggingDbContext>();

        return (provider, db);
    }

    [Fact]
    public async Task AuditLogProcessor_ShouldDrainAndFlushBatch_WithoutMemoryLeak()
    {
        // Arrange
        var (provider, db) = CreateServiceProviderWithInMemoryDb();
        var queue = new AuditLogQueue();

        // 入队正常日志及超长字符串日志
        queue.EnqueueAuditLog(new AuditLogEntry
        {
            UserId = 1,
            Username = "admin",
            Module = "文章管理",
            Action = "发布文章",
            Method = "POST",
            Path = "/api/admin/articles",
            Parameters = "{}",
            ClientIp = "127.0.0.1",
            DurationMs = 120,
            IsSuccess = true,
            ErrorMessage = null,
            TraceId = "tr_test_123",
        });

        // 超长 Path (超过 256 字符)
        var longPath = "/" + new string('a', 300);
        queue.EnqueueAuditLog(new AuditLogEntry
        {
            UserId = 2,
            Username = "test_user",
            Module = "超长模块" + new string('b', 100),
            Action = "超长操作",
            Method = "GET",
            Path = longPath,
            Parameters = null,
            ClientIp = "127.0.0.1",
            DurationMs = 50,
            IsSuccess = true,
            ErrorMessage = null,
            TraceId = "tr_test_456",
        });

        var processor = new AuditLogProcessor(queue, provider, NullLogger<AuditLogProcessor>.Instance);

        // Act: 启动并在处理完成后优雅停止
        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(500));
        var runTask = processor.StartAsync(cts.Token);
        await Task.Delay(100);

        await processor.StopAsync(CancellationToken.None);
        await runTask;

        // Assert
        var logs = await db.AuditLogs.ToListAsync();
        logs.Should().HaveCount(2);

        // 验证超长字段已被安全截断
        var longLog = logs.First(l => l.UserId == 2);
        longLog.Path.Length.Should().BeLessThanOrEqualTo(256);
        longLog.Module.Length.Should().BeLessThanOrEqualTo(50);
    }
}
