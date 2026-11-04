namespace Content.Application.Dashboard.DTOs;

/// <summary>
/// 日活跃度趋势项
/// </summary>
public record TrendItemDto
{
    /// <summary>日期字符串（如 03-28）</summary>
    public string Date { get; init; } = string.Empty;

    /// <summary>当日发布文章数</summary>
    public int ArticleCount { get; init; }
}
