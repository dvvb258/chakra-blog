namespace Content.Application.Series.DTOs;

/// <summary>
/// 系列列表项DTO（含文章数量）
/// </summary>
public record SeriesListItemDto
{
    /// <summary>系列ID</summary>
    public long Id { get; init; }

    /// <summary>系列名称</summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>系列描述</summary>
    public string Description { get; init; } = string.Empty;

    /// <summary>封面图片URL</summary>
    public string CoverUrl { get; init; } = string.Empty;

    /// <summary>主题色</summary>
    public string Theme { get; init; } = string.Empty;

    /// <summary>作者</summary>
    public string Author { get; init; } = string.Empty;

    /// <summary>文章数量</summary>
    public int ArticleCount { get; init; }

    /// <summary>创建时间</summary>
    public DateTime CreatedAt { get; init; }

    /// <summary>更新时间</summary>
    public DateTime? UpdatedAt { get; init; }

    /// <summary>版本号</summary>
    public int Version { get; init; }
}
