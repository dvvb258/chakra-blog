namespace Content.Application.Articles.DTOs;

/// <summary>
/// 相邻文章项DTO（仅包含导航所需的ID与标题）
/// </summary>
public record ArticleNeighborItemDto
{
    /// <summary>文章ID</summary>
    public long Id { get; init; }

    /// <summary>文章标题</summary>
    public string Title { get; init; } = string.Empty;
}
