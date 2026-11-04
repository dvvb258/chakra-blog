namespace ApiHost.DTOs;

/// <summary>
/// 系统核心功能开关状态
/// </summary>
public sealed record SystemFeaturesDto
{
    /// <summary>
    /// 是否开启 GitHub 快捷登录与账号绑定
    /// </summary>
    public bool GithubLogin { get; init; }

    /// <summary>
    /// 是否开启 AI 辅助生成能力（取决于是否配置有效 API Key）
    /// </summary>
    public bool AiAssistant { get; init; }

    /// <summary>
    /// 是否开启后台 AI 定时自动发文调度
    /// </summary>
    public bool AutoGenerateArticle { get; init; }

    /// <summary>
    /// 媒体文件存储模式：Local（本地磁盘静态目录）或 S3
    /// </summary>
    public string StorageProvider { get; init; } = "Local";

    /// <summary>
    /// 是否开启 Redis 分布式缓存与并发锁（未开启时已自动降级为单机内存模式）
    /// </summary>
    public bool RedisEnabled { get; init; }
}
