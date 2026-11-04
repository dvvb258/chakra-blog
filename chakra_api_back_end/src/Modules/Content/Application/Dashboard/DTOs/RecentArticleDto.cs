namespace Content.Application.Dashboard.DTOs;

/// <summary>
/// 最新文章项 DTO
/// </summary>
public record RecentArticleDto
{
    /// <summary>文章ID</summary>
    public long Id { get; init; }

    /// <summary>文章标题</summary>
    public string Title { get; init; } = string.Empty;

    /// <summary>所属分类</summary>
    public string Category { get; init; } = string.Empty;

    /// <summary>阅读量</summary>
    public long Views { get; init; }

    /// <summary>作者</summary>
    public string Author { get; init; } = string.Empty;

    /// <summary>发布时间</summary>
    public DateTime CreatedAt { get; init; }
}
