namespace SharedKernel.Application;

/// <summary>
/// 异步日志内存通道队列接口（基于 System.Threading.Channels 高性能非阻塞通道）
/// </summary>
public interface IAuditLogQueue
{
    /// <summary>
    /// 将操作审计日志压入内存缓冲队列（微秒级非阻塞）
    /// </summary>
    /// <param name="entry">审计日志条目</param>
    /// <returns>是否成功入队</returns>
    bool EnqueueAuditLog(AuditLogEntry entry);

    /// <summary>
    /// 将系统异常日志压入内存缓冲队列（微秒级非阻塞）
    /// </summary>
    /// <param name="entry">异常日志条目</param>
    /// <returns>是否成功入队</returns>
    bool EnqueueExceptionLog(ExceptionLogEntry entry);

    /// <summary>
    /// 因队列满而丢弃的审计日志数量
    /// </summary>
    long DroppedAuditLogCount { get; }

    /// <summary>
    /// 因队列满而丢弃的异常日志数量
    /// </summary>
    long DroppedExceptionLogCount { get; }

    /// <summary>
    /// 异步读取待处理的审计日志流
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>审计日志异步流</returns>
    IAsyncEnumerable<AuditLogEntry> ReadAuditLogsAsync(CancellationToken cancellationToken);

    /// <summary>
    /// 异步读取待处理的异常日志流
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>异常日志异步流</returns>
    IAsyncEnumerable<ExceptionLogEntry> ReadExceptionLogsAsync(CancellationToken cancellationToken);

    /// <summary>
    /// 完成队列写入，使后台处理器可以排空剩余日志后退出
    /// </summary>
    void Complete();
}
