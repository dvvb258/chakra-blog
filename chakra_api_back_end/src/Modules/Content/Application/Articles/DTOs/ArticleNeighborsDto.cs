namespace Content.Application.Articles.DTOs;

/// <summary>
/// 文章相邻上下篇导航DTO
/// </summary>
public record ArticleNeighborsDto
{
    /// <summary>上一篇文章（若不存在为null）</summary>
    public ArticleNeighborItemDto? Prev { get; init; }

    /// <summary>下一篇文章（若不存在为null）</summary>
    public ArticleNeighborItemDto? Next { get; init; }
}
