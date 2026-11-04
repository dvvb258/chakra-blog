namespace Content.Application.Series.DTOs;

/// <summary>
/// 更新专栏系列请求体
/// </summary>
public sealed record UpdateSeriesRequest
{
    /// <summary>
    /// 专栏系列名称
    /// </summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>
    /// 专栏描述
    /// </summary>
    public string? Description { get; init; }

    /// <summary>
    /// 封面图片 URL
    /// </summary>
    public string? CoverUrl { get; init; }

    /// <summary>
    /// 主题色彩
    /// </summary>
    public string? Theme { get; init; }

    /// <summary>
    /// 客户端持有的版本号（用于乐观并发锁校验，可选）
    /// </summary>
    public int? Version { get; init; }
}
