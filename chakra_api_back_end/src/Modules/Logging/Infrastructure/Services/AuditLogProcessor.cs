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
                    try
                    {
                        await FlushAuditBatchAsync(batch);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "【日志系统】写入审计日志批次发生异常，已安全丢弃本批次 {Count} 条日志以防止内存累积死循环", batch.Count);
                    }
                    finally
                    {
                        batch.Clear();
                    }
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex) when (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogError(ex, "【日志系统】审计日志处理循环发生异常");
                await Task.Delay(1000, stoppingToken);
            }
        }

        await foreach (var item in _queue.ReadAuditLogsAsync(CancellationToken.None))
        {
            batch.Add(item);
            if (batch.Count >= BatchSize)
            {
                try
                {
                    await FlushAuditBatchAsync(batch);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "【日志系统】停机时写入审计日志批次发生异常");
                }
                finally
                {
                    batch.Clear();
                }
            }
        }

        if (batch.Count > 0)
        {
            try
            {
                await FlushAuditBatchAsync(batch);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "【日志系统】停机时写入剩余审计日志批次发生异常");
            }
            finally
            {
                batch.Clear();
            }
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
                    try
                    {
                        await FlushExceptionBatchAsync(batch);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "【日志系统】写入异常日志批次发生异常，已安全丢弃本批次 {Count} 条日志以防止内存累积死循环", batch.Count);
                    }
                    finally
                    {
                        batch.Clear();
                    }
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex) when (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogError(ex, "【日志系统】异常日志处理循环发生异常");
                await Task.Delay(1000, stoppingToken);
            }
        }

        await foreach (var item in _queue.ReadExceptionLogsAsync(CancellationToken.None))
        {
            batch.Add(item);
            if (batch.Count >= BatchSize)
            {
                try
                {
                    await FlushExceptionBatchAsync(batch);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "【日志系统】停机时写入异常日志批次发生异常");
                }
                finally
                {
                    batch.Clear();
                }
            }
        }

        if (batch.Count > 0)
        {
            try
            {
                await FlushExceptionBatchAsync(batch);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "【日志系统】停机时写入剩余异常日志批次发生异常");
            }
            finally
            {
                batch.Clear();
            }
        }
    }

    private async Task FlushAuditBatchAsync(List<AuditLogEntry> batch)
    {
        using var scope = _serviceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<LoggingDbContext>();

        var entities = batch.Select(e => new AuditLog(
            userId: e.UserId,
            username: Truncate(e.Username, 50),
            module: Truncate(e.Module, 50) ?? string.Empty,
            action: Truncate(e.Action, 100) ?? string.Empty,
            method: Truncate(e.Method, 10) ?? string.Empty,
            path: Truncate(e.Path, 256) ?? string.Empty,
            parameters: e.Parameters,
            clientIp: Truncate(e.ClientIp, 50),
            durationMs: e.DurationMs,
            isSuccess: e.IsSuccess,
            errorMessage: e.ErrorMessage,
            traceId: Truncate(e.TraceId, 64))).ToList();

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
            exceptionType: Truncate(e.ExceptionType, 200) ?? string.Empty,
            message: e.Message ?? string.Empty,
            stackTrace: e.StackTrace,
            path: Truncate(e.Path, 256) ?? string.Empty,
            method: Truncate(e.Method, 10) ?? string.Empty,
            queryString: Truncate(e.QueryString, 500),
            requestBody: e.RequestBody,
            clientIp: Truncate(e.ClientIp, 50),
            userId: e.UserId,
            username: Truncate(e.Username, 50),
            traceId: Truncate(e.TraceId, 64))).ToList();

        await db.ExceptionLogs.AddRangeAsync(entities);
        await db.SaveChangesAsync();
        _logger.LogDebug("【日志系统】已批量持久化 {Count} 条系统异常日志", batch.Count);
        ReportDroppedLogs();
    }

    private static string? Truncate(string? value, int maxLength)
    {
        if (string.IsNullOrEmpty(value) || value.Length <= maxLength)
        {
            return value;
        }

        return value[..maxLength];
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
