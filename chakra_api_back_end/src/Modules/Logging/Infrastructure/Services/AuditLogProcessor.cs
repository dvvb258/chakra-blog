using Logging.Domain.Entities;
using Logging.Infrastructure.Data;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SharedKernel.Application;

namespace Logging.Infrastructure.Services;

/// <summary>
/// 后台日志批量持久化服务（每 2 秒或积攒 50 条即批量写入 PostgreSQL）
/// </summary>
public sealed class AuditLogProcessor : BackgroundService
{
    private readonly IAuditLogQueue _queue;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<AuditLogProcessor> _logger;
    private long _reportedDroppedAuditLogs;
    private long _reportedDroppedExceptionLogs;

    private const int BatchSize = 50;
    private static readonly TimeSpan FlushInterval = TimeSpan.FromSeconds(2);

    public AuditLogProcessor(
        IAuditLogQueue queue,
        IServiceProvider serviceProvider,
        ILogger<AuditLogProcessor> logger)
    {
        _queue = queue;
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("【日志系统】异步日志落库 BackgroundService 已启动");

        var auditTask = ProcessAuditLogsAsync(stoppingToken);
        var exceptionTask = ProcessExceptionLogsAsync(stoppingToken);

        await Task.WhenAll(auditTask, exceptionTask);
    }

    /// <inheritdoc />
    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _queue.Complete();
        await base.StopAsync(cancellationToken);
    }

    private async Task ProcessAuditLogsAsync(CancellationToken stoppingToken)
    {
        var batch = new List<AuditLogEntry>(BatchSize);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var cts = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);
                cts.CancelAfter(FlushInterval);

                try
                {
                    await foreach (var item in _queue.ReadAuditLogsAsync(cts.Token))
                    {
                        batch.Add(item);
                        if (batch.Count >= BatchSize)
                        {
                            break;
                        }
                    }
                }
                catch (OperationCanceledException) when (!stoppingToken.IsCancellationRequested)
                {
                    // 定时触发批量写入
                }

                if (batch.Count > 0)
                {
                    await FlushAuditBatchAsync(batch);
                    batch.Clear();
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex) when (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogError(ex, "【日志系统】写入审计日志批次发生异常");
                await Task.Delay(1000, stoppingToken);
            }
        }

        await foreach (var item in _queue.ReadAuditLogsAsync(CancellationToken.None))
        {
            batch.Add(item);
            if (batch.Count >= BatchSize)
            {
                await FlushAuditBatchAsync(batch);
                batch.Clear();
            }
        }

        if (batch.Count > 0)
        {
            await FlushAuditBatchAsync(batch);
        }
    }

    private async Task ProcessExceptionLogsAsync(CancellationToken stoppingToken)
    {
        var batch = new List<ExceptionLogEntry>(BatchSize);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var cts = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);
                cts.CancelAfter(FlushInterval);

                try
                {
                    await foreach (var item in _queue.ReadExceptionLogsAsync(cts.Token))
                    {
                        batch.Add(item);
                        if (batch.Count >= BatchSize)
                        {
                            break;
                        }
                    }
                }
                catch (OperationCanceledException) when (!stoppingToken.IsCancellationRequested)
                {
                    // 定时触发批量写入
                }

                if (batch.Count > 0)
                {
                    await FlushExceptionBatchAsync(batch);
                    batch.Clear();
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex) when (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogError(ex, "【日志系统】写入异常日志批次发生异常");
                await Task.Delay(1000, stoppingToken);
            }
        }

        await foreach (var item in _queue.ReadExceptionLogsAsync(CancellationToken.None))
        {
            batch.Add(item);
            if (batch.Count >= BatchSize)
            {
                await FlushExceptionBatchAsync(batch);
                batch.Clear();
            }
        }

        if (batch.Count > 0)
        {
            await FlushExceptionBatchAsync(batch);
        }
    }

    private async Task FlushAuditBatchAsync(List<AuditLogEntry> batch)
    {
        using var scope = _serviceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<LoggingDbContext>();

        var entities = batch.Select(e => new AuditLog(
            userId: e.UserId,
            username: e.Username,
            module: e.Module,
            action: e.Action,
            method: e.Method,
            path: e.Path,
            parameters: e.Parameters,
            clientIp: e.ClientIp,
            durationMs: e.DurationMs,
            isSuccess: e.IsSuccess,
            errorMessage: e.ErrorMessage,
            traceId: e.TraceId)).ToList();

        await db.AuditLogs.AddRangeAsync(entities);
        await db.SaveChangesAsync();
        _logger.LogDebug("【日志系统】已批量持久化 {Count} 条操作审计日志", batch.Count);
        ReportDroppedLogs();
    }

    private async Task FlushExceptionBatchAsync(List<ExceptionLogEntry> batch)
    {
        using var scope = _serviceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<LoggingDbContext>();

        var entities = batch.Select(e => new ExceptionLog(
            exceptionType: e.ExceptionType,
            message: e.Message,
            stackTrace: e.StackTrace,
            path: e.Path,
            method: e.Method,
            queryString: e.QueryString,
            requestBody: e.RequestBody,
            clientIp: e.ClientIp,
            userId: e.UserId,
            username: e.Username,
            traceId: e.TraceId)).ToList();

        await db.ExceptionLogs.AddRangeAsync(entities);
        await db.SaveChangesAsync();
        _logger.LogDebug("【日志系统】已批量持久化 {Count} 条系统异常日志", batch.Count);
        ReportDroppedLogs();
    }

    private void ReportDroppedLogs()
    {
        var droppedAuditLogs = _queue.DroppedAuditLogCount;
        var droppedExceptionLogs = _queue.DroppedExceptionLogCount;
        if (droppedAuditLogs == _reportedDroppedAuditLogs &&
            droppedExceptionLogs == _reportedDroppedExceptionLogs)
        {
            return;
        }

        _reportedDroppedAuditLogs = droppedAuditLogs;
        _reportedDroppedExceptionLogs = droppedExceptionLogs;
        _logger.LogWarning(
            "【日志系统】内存队列容量不足，累计丢弃审计日志 {AuditCount} 条、异常日志 {ExceptionCount} 条",
            droppedAuditLogs,
            droppedExceptionLogs);
    }
}
