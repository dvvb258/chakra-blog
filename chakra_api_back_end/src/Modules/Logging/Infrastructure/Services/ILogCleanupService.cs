namespace Logging.Infrastructure.Services;

/// <summary>
/// 过期日志自动清理服务契约
/// </summary>
public interface ILogCleanupService
{
    /// <summary>
    /// 清理早于指定天数的历史审计日志与异常日志
    /// </summary>
    /// <param name="retentionDays">日志保留天数（例如 60 天）</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>删除的审计日志与异常日志条数</returns>
    Task<(int DeletedAudits, int DeletedExceptions)> CleanupExpiredLogsAsync(int retentionDays, CancellationToken cancellationToken = default);
}
