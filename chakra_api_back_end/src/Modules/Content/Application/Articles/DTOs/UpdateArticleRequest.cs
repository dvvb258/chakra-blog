namespace Content.Application.Articles.DTOs;

/// <summary>
/// 更新文章请求体
/// </summary>
public sealed record UpdateArticleRequest
{
    /// <summary>
    /// 文章标题
    /// </summary>
    public string Title { get; init; } = string.Empty;

    /// <summary>
    /// Markdown 正文内容
    /// </summary>
    public string Content { get; init; } = string.Empty;

    /// <summary>
    /// 文章摘要（可选，为空自动提取）
    /// </summary>
    public string? Summary { get; init; }

    /// <summary>
    /// 标签集合
    /// </summary>
    public List<string>? Tags { get; init; }

    /// <summary>
    /// 所属分类ID
    /// </summary>
    public long CategoryId { get; init; }

    /// <summary>
    /// 作者（可选）
    /// </summary>
    public string? Author { get; init; }

    /// <summary>
    /// 预估阅读时间（分钟，可选，为0自动计算）
    /// </summary>
    public int ReadingTime { get; init; }

    /// <summary>
    /// 封面色彩
    /// </summary>
    public string? CoverColor { get; init; }

    /// <summary>
    /// 封面图片 URL
    /// </summary>
    public string? CoverUrl { get; init; }

    /// <summary>
    /// 归属专栏系列ID（可选）
    /// </summary>
    public long? SeriesId { get; init; }

    /// <summary>
    /// 文章状态变更（可选）
    /// </summary>
    public int? Status { get; init; }

    /// <summary>
    /// 客户端持有的版本号（用于乐观并发锁校验，可选）
    /// </summary>
    public int? Version { get; init; }
}
