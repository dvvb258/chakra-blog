namespace Content.Application.Series.DTOs;

/// <summary>
/// 创建专栏系列请求体
/// </summary>
public sealed record CreateSeriesRequest
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
    /// 主题色彩（例如 #0284c7 或渐变值）
    /// </summary>
    public string? Theme { get; init; }
}
