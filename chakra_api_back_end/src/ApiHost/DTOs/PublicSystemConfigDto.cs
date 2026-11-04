namespace ApiHost.DTOs;

/// <summary>
/// 系统公共特性探测响应数据传输对象
/// </summary>
public sealed record PublicSystemConfigDto
{
    /// <summary>
    /// 系统功能特性状态
    /// </summary>
    public SystemFeaturesDto Features { get; init; } = new();

    /// <summary>
    /// 站点基本信息
    /// </summary>
    public SystemInfoDto Site { get; init; } = new();
}
