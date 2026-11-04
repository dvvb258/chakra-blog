namespace Content.Application.Articles.DTOs;

/// <summary>
/// 文章列表项DTO（不含正文）
/// </summary>
public record ArticleListItemDto
{
    /// <summary>文章ID</summary>
    public long Id { get; init; }

    /// <summary>文章标题</summary>
    public string Title { get; init; } = string.Empty;

    /// <summary>文章摘要</summary>
    public string Summary { get; init; } = string.Empty;

    /// <summary>标签列表</summary>
    public string[] Tags { get; init; } = [];

    /// <summary>所属分类ID</summary>
    public long CategoryId { get; init; }

    /// <summary>分类显示名称（如 .NET / C#）</summary>
    public string CategoryName { get; init; } = string.Empty;

    /// <summary>作者</summary>
    public string Author { get; init; } = string.Empty;

    /// <summary>阅读时间（分钟）</summary>
    public int ReadingTime { get; init; }

    /// <summary>封面颜色</summary>
    public string CoverColor { get; init; } = string.Empty;

    /// <summary>封面图片链接</summary>
    public string CoverUrl { get; init; } = string.Empty;

    /// <summary>阅读量</summary>
    public long Views { get; init; }

    /// <summary>点赞数</summary>
    public long Likes { get; init; }

    /// <summary>所属系列ID</summary>
    public long? SeriesId { get; init; }

    /// <summary>创建人用户ID</summary>
    public long CreatedByUserId { get; init; }

    /// <summary>文章状态（0: 草稿, 1: 已发布, 2: 待审核, 3: 已退回）</summary>
    public int Status { get; init; } = 1;

    /// <summary>创建时间</summary>
    public DateTime CreatedAt { get; init; }

    /// <summary>更新时间</summary>
    public DateTime? UpdatedAt { get; init; }

    /// <summary>版本号</summary>
    public int Version { get; init; }
}
