namespace ApiHost.DTOs;

/// <summary>
/// 站点基本元数据
/// </summary>
public sealed record SystemInfoDto
{
    /// <summary>
    /// 博客站点名称
    /// </summary>
    public string SiteName { get; init; } = "Chakra Blog System";

    /// <summary>
    /// 当前后端系统架构版本号
    /// </summary>
    public string Version { get; init; } = "1.0.0";
}
