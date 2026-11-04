using Content.Application.Articles.DTOs;

namespace Content.Application.Series.DTOs;

/// <summary>
/// 系列详情DTO（含文章列表，文章不含正文）
/// </summary>
public record SeriesDetailDto
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

    /// <summary>创建时间</summary>
    public DateTime CreatedAt { get; init; }

    /// <summary>更新时间</summary>
    public DateTime? UpdatedAt { get; init; }

    /// <summary>版本号</summary>
    public int Version { get; init; }

    /// <summary>系列文章列表（不含正文，按创建时间升序）</summary>
    public List<ArticleListItemDto> Articles { get; init; } = [];
}
