using System.Threading.Channels;
using SharedKernel.Application;

namespace Logging.Infrastructure.Services;

/// <summary>
/// 异步日志内存缓冲队列实现（基于 System.Threading.Channels 高性能有界无锁队列）
/// </summary>
public sealed class AuditLogQueue : IAuditLogQueue
{
    private readonly Channel<AuditLogEntry> _auditChannel;
    private readonly Channel<ExceptionLogEntry> _exceptionChannel;
    private long _droppedAuditLogCount;
    private long _droppedExceptionLogCount;

    /// <inheritdoc />
    public long DroppedAuditLogCount => Interlocked.Read(ref _droppedAuditLogCount);

    /// <inheritdoc />
    public long DroppedExceptionLogCount => Interlocked.Read(ref _droppedExceptionLogCount);

    public AuditLogQueue()
    {
        // 审计日志通道：容量上限 10000 条，满时由 TryWrite 失败并由调用方统计丢弃量
        _auditChannel = Channel.CreateBounded<AuditLogEntry>(new BoundedChannelOptions(10000)
        {
            FullMode = BoundedChannelFullMode.Wait,
            SingleWriter = false,
            SingleReader = true
        });

        // 异常日志通道：容量上限 5000 条，满时由 TryWrite 失败并由调用方统计丢弃量
        _exceptionChannel = Channel.CreateBounded<ExceptionLogEntry>(new BoundedChannelOptions(5000)
        {
            FullMode = BoundedChannelFullMode.Wait,
            SingleWriter = false,
            SingleReader = true
        });
    }

    /// <inheritdoc />
    public bool EnqueueAuditLog(AuditLogEntry entry)
    {
        if (_auditChannel.Writer.TryWrite(entry))
        {
            return true;
        }

        Interlocked.Increment(ref _droppedAuditLogCount);
        return false;
    }

    /// <inheritdoc />
    public bool EnqueueExceptionLog(ExceptionLogEntry entry)
    {
        if (_exceptionChannel.Writer.TryWrite(entry))
        {
            return true;
        }

        Interlocked.Increment(ref _droppedExceptionLogCount);
        return false;
    }

    /// <inheritdoc />
    public IAsyncEnumerable<AuditLogEntry> ReadAuditLogsAsync(CancellationToken cancellationToken)
    {
        return _auditChannel.Reader.ReadAllAsync(cancellationToken);
    }

    /// <inheritdoc />
    public IAsyncEnumerable<ExceptionLogEntry> ReadExceptionLogsAsync(CancellationToken cancellationToken)
    {
        return _exceptionChannel.Reader.ReadAllAsync(cancellationToken);
    }

    /// <inheritdoc />
    public void Complete()
    {
        _auditChannel.Writer.TryComplete();
        _exceptionChannel.Writer.TryComplete();
    }
}
