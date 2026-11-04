namespace SharedKernel.Constants;

/// <summary>
/// Redis 键名常量集中定义
/// </summary>
public static class RedisKeys
{
    /// <summary>
    /// 系统总访问量键名
    /// </summary>
    public const string StatsSystemVisits = "stats:system:visits";

    /// <summary>
    /// 获取指定日期 AI 文章生成计数器键名
    /// </summary>
    /// <param name="date">北京时间日期</param>
    /// <returns>Redis 键名</returns>
    public static string ArticleGenerationCount(DateTime date) =>
        $"article:generation:count:{date:yyyy-MM-dd}";

    /// <summary>
    /// 文章生成全局分布式互斥执行锁键名（确保同一时间仅有单一 Pod 实例在执行文章生成任务）
    /// </summary>
    public const string ArticleGenerationExecutionLock = "lock:article:generation:job";

    /// <summary>
    /// 文章源链接处理认领分布式锁键名（防止极端并发下选到相同的文章源）
    /// </summary>
    /// <param name="linkHash">链接哈希标识</param>
    /// <returns>Redis 键名</returns>
    public static string ArticleSourceLinkLock(string linkHash) =>
        $"lock:article:sourcelink:{linkHash}";
}
