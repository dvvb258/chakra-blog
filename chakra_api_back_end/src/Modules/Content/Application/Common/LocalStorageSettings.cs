namespace Content.Application.Common;

/// <summary>
/// 本地磁盘静态文件存储配置
/// </summary>
public sealed class LocalStorageSettings
{
    /// <summary>
    /// 配置节点名称
    /// </summary>
    public const string SectionName = "Storage:Local";

    /// <summary>
    /// 本地文件物理存储子目录（相对于宿主根目录），默认 "uploads"
    /// </summary>
    public string UploadDir { get; set; } = "uploads";

    /// <summary>
    /// 静态资源公开访问前缀路径（URL Path），默认 "/uploads"
    /// </summary>
    public string RequestPath { get; set; } = "/uploads";

    /// <summary>
    /// 公共访问基础域名（如 https://img.example.com；若未配置则直接返回以 /uploads 开头的相对路径，彻底解耦域名与端口）
    /// </summary>
    public string? PublicUrl { get; set; }
}
