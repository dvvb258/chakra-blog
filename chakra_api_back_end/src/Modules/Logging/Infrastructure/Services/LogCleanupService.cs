using Logging.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Logging.Infrastructure.Services;

/// <summary>
/// 过期日志自动清理服务实现
/// </summary>
public sealed class LogCleanupService : ILogCleanupService
{
    private readonly LoggingDbContext _dbContext;
    private readonly ILogger<LogCleanupService> _logger;

    public LogCleanupService(LoggingDbContext dbContext, ILogger<LogCleanupService> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<(int DeletedAudits, int DeletedExceptions)> CleanupExpiredLogsAsync(int retentionDays, CancellationToken cancellationToken = default)
    {
        if (retentionDays <= 0)
        {
            _logger.LogDebug("日志保留天数配置为 {RetentionDays}，跳过日志自动清理。", retentionDays);
            return (0, 0);
        }

        var cutoff = DateTime.UtcNow.AddDays(-retentionDays);
        int deletedAudits;
        int deletedExceptions;

        var isInMemory = _dbContext.Database.ProviderName?.EndsWith("InMemory", StringComparison.OrdinalIgnoreCase) == true;

        if (isInMemory)
        {
            var expiredAudits = await _dbContext.AuditLogs
                .Where(x => x.CreatedAt < cutoff)
                .ToListAsync(cancellationToken);
            _dbContext.AuditLogs.RemoveRange(expiredAudits);
            deletedAudits = expiredAudits.Count;

            var expiredExceptions = await _dbContext.ExceptionLogs
                .Where(x => x.CreatedAt < cutoff)
                .ToListAsync(cancellationToken);
            _dbContext.ExceptionLogs.RemoveRange(expiredExceptions);
            deletedExceptions = expiredExceptions.Count;

            await _dbContext.SaveChangesAsync(cancellationToken);
        }
        else
        {
            deletedAudits = await _dbContext.AuditLogs
                .Where(x => x.CreatedAt < cutoff)
                .ExecuteDeleteAsync(cancellationToken);

            deletedExceptions = await _dbContext.ExceptionLogs
                .Where(x => x.CreatedAt < cutoff)
                .ExecuteDeleteAsync(cancellationToken);
        }

        if (deletedAudits > 0 || deletedExceptions > 0)
        {
            _logger.LogInformation(
                "已完成过期日志自动清理: 删除了 {DeletedAudits} 条审计日志与 {DeletedExceptions} 条异常日志 (保留天数: {RetentionDays} 天, 截止时间: {Cutoff:yyyy-MM-dd HH:mm:ss} UTC)。",
                deletedAudits,
                deletedExceptions,
                retentionDays,
                cutoff);
        }

        return (deletedAudits, deletedExceptions);
    }
}
