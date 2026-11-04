namespace Content.Application.Common;

/// <summary>
/// 全站访问量与统计追踪服务契约（支持 Redis 与单机内存优雅降级）
/// </summary>
public interface ISystemStatsTracker
{
    /// <summary>
    /// 系统总访问量原子自增 1
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    Task IncrementSystemVisitsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取全站累计总访问量
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>全站累计访问量数值</returns>
    Task<long> GetSystemVisitsAsync(CancellationToken cancellationToken = default);
}
