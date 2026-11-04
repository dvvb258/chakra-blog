namespace Content.Application.Dashboard.DTOs;

/// <summary>
/// 分类文章统计
/// </summary>
public record CategoryStatDto
{
    /// <summary>分类名称</summary>
    public string Category { get; init; } = string.Empty;

    /// <summary>该分类下的文章篇数</summary>
    public int Count { get; init; }
}
