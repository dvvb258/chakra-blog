using SharedKernel.Domain;

namespace Content.Domain.Entities;

/// <summary>
/// 文章匿名点赞关系实体（同一访客对同一文章只保留一条有效记录）
/// </summary>
public class ArticleLike : Entity
{
    /// <summary>
    /// 被点赞的文章 ID
    /// </summary>
    public long ArticleId { get; private set; }

    /// <summary>
    /// 匿名访客的哈希标识
    /// </summary>
    public string VisitorId { get; private set; } = string.Empty;

    private ArticleLike()
        : base()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ArticleLike"/> class.
    /// </summary>
    /// <param name="articleId">文章 ID</param>
    /// <param name="visitorId">匿名访客哈希标识</param>
    public ArticleLike(long articleId, string visitorId)
        : base(SnowflakeIdGenerator.NextId())
    {
        ArticleId = articleId;
        VisitorId = visitorId;
    }
}
