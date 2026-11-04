namespace Content.Application.Common;

/// <summary>
/// 媒体存储总控配置
/// </summary>
public sealed class StorageSettings
{
    /// <summary>
    /// 配置节点名称
    /// </summary>
    public const string SectionName = "Storage";

    /// <summary>
    /// 存储提供者：Local（默认本地磁盘）或 S3（通用 S3 兼容协议对象存储）
    /// </summary>
    public string Provider { get; set; } = "Local";
}
