using Content.Domain.Enums;
using SharedKernel.Domain;

namespace Content.Domain.Entities;

/// <summary>
/// 文章实体
/// </summary>
public class Article : Entity, IAggregateRoot
{
    /// <summary>
    /// 文章标题
    /// </summary>
    public string Title { get; private set; } = string.Empty;

    /// <summary>
    /// 文章正文内容
    /// </summary>
    public string Content { get; private set; } = string.Empty;

    /// <summary>
    /// 文章摘要
    /// </summary>
    public string Summary { get; private set; } = string.Empty;

    /// <summary>
    /// 阅读时间（分钟）
    /// </summary>
    public int ReadingTime { get; private set; }

    /// <summary>
    /// 标签列表
    /// </summary>
    public List<string> Tags { get; private set; } = [];

    /// <summary>
    /// 所属分类ID
    /// </summary>
    public long CategoryId { get; private set; }

    /// <summary>
    /// 作者
    /// </summary>
    public string Author { get; private set; } = string.Empty;

    /// <summary>
    /// 封面颜色
    /// </summary>
    public string CoverColor { get; private set; } = string.Empty;

    /// <summary>
    /// 封面图片 URL（支持 S3 兼容图床等外部存储）
    /// </summary>
    public string CoverUrl { get; private set; } = string.Empty;

    /// <summary>
    /// 阅读量
    /// </summary>
    public long Views { get; private set; }

    /// <summary>
    /// 点赞数
    /// </summary>
    public long Likes { get; private set; }

    /// <summary>
    /// 所属系列ID
    /// </summary>
    public long? SeriesId { get; private set; }

    /// <summary>
    /// 是否置顶展示（首页精选与置顶优先展示）
    /// </summary>
    public bool IsPinned { get; private set; }

    /// <summary>
    /// 原文来源链接（AI生成文章使用，用于去重判断）
    /// </summary>
    public string? SourceLink { get; private set; }

    /// <summary>
    /// 创建人用户ID（0 表示系统后台AI生成）
    /// </summary>
    public long CreatedByUserId { get; private set; }

    /// <summary>
    /// 文章审核与发布状态
    /// </summary>
    public ArticleStatus Status { get; private set; } = ArticleStatus.Draft;

    private Article()
        : base()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Article"/> class.
    /// </summary>
    /// <param name="title">文章标题</param>
    /// <param name="content">文章正文</param>
    /// <param name="summary">文章摘要</param>
    /// <param name="tags">标签集合</param>
    /// <param name="categoryId">所属分类ID</param>
    /// <param name="author">署名作者</param>
    /// <param name="readingTime">预估阅读时间</param>
    /// <param name="coverColor">封面强调色</param>
    /// <param name="coverUrl">封面图片地址</param>
    /// <param name="sourceLink">原文引用链接</param>
    /// <param name="createdByUserId">创建人用户ID</param>
    /// <param name="status">初始文章状态</param>
    /// <param name="isPinned">是否置顶展示</param>
    public Article(
        string title,
        string content,
        string summary = "",
        List<string>? tags = null,
        long categoryId = 0,
        string author = "",
        int readingTime = 0,
        string coverColor = "",
        string coverUrl = "",
        string? sourceLink = null,
        long createdByUserId = 0,
        ArticleStatus status = ArticleStatus.Draft,
        bool isPinned = false)
        : base(SnowflakeIdGenerator.NextId())
    {
        Title = title;
        Content = content;
        Summary = summary;
        ReadingTime = readingTime > 0 ? readingTime : CalculateReadingTime(content);
        Tags = tags ?? [];
        CategoryId = categoryId;
        Author = author;
        CoverColor = coverColor;
        CoverUrl = coverUrl?.Trim() ?? string.Empty;
        SourceLink = sourceLink;
        CreatedByUserId = createdByUserId;
        Status = status;
        IsPinned = isPinned;
    }

    /// <summary>
    /// 更新文章主体信息
    /// </summary>
    /// <param name="title">文章标题</param>
    /// <param name="content">文章正文</param>
    /// <param name="summary">文章摘要</param>
    /// <param name="tags">标签集合</param>
    /// <param name="categoryId">分类ID</param>
    /// <param name="author">作者</param>
    /// <param name="readingTime">预估阅读时间</param>
    /// <param name="coverColor">封面色值</param>
    /// <param name="coverUrl">封面图片链接</param>
    /// <param name="seriesId">所属系列ID</param>
    /// <param name="isPinned">是否置顶（可选）</param>
    public void Update(
        string title,
        string content,
        string summary,
        List<string>? tags,
        long categoryId,
        string author,
        int readingTime,
        string coverColor,
        string? coverUrl,
        long? seriesId,
        bool? isPinned = null)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            throw new ArgumentException("文章标题不能为空", nameof(title));
        }

        if (string.IsNullOrWhiteSpace(content))
        {
            throw new ArgumentException("文章内容不能为空", nameof(content));
        }

        Title = title.Trim();
        Content = content;
        Summary = string.IsNullOrWhiteSpace(summary) ? ExtractSummary(content) : summary.Trim();
        Tags = tags ?? [];
        CategoryId = categoryId;
        Author = string.IsNullOrWhiteSpace(author) ? Author : author.Trim();
        ReadingTime = readingTime > 0 ? readingTime : CalculateReadingTime(content);
        CoverColor = coverColor ?? string.Empty;
        CoverUrl = coverUrl?.Trim() ?? string.Empty;
        SeriesId = seriesId.HasValue && seriesId.Value > 0 ? seriesId.Value : null;
        if (isPinned.HasValue)
        {
            IsPinned = isPinned.Value;
        }

        SetUpdatedAt();
    }

    /// <summary>
    /// 快速切换或设置文章置顶状态
    /// </summary>
    /// <param name="isPinned">是否置顶</param>
    public void SetPinned(bool isPinned)
    {
        if (IsPinned != isPinned)
        {
            IsPinned = isPinned;
            SetUpdatedAt();
        }
    }

    /// <summary>
    /// 增加文章点赞数
    /// </summary>
    public void IncrementLikes()
    {
        Likes++;
    }

    /// <summary>
    /// 减少文章点赞数（非负保底）
    /// </summary>
    public void DecrementLikes()
    {
        if (Likes > 0)
        {
            Likes--;
        }
    }

    /// <summary>
    /// 自动从正文中提取纯文本作为摘要
    /// </summary>
    /// <param name="content">Markdown 正文</param>
    /// <returns>提取的纯文本摘要</returns>
    public static string ExtractSummary(string content)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            return string.Empty;
        }

        // 1. 移除多行代码块
        var clean = System.Text.RegularExpressions.Regex.Replace(content, @"```[\s\S]*?```", " ");

        // 2. 移除 HTML 标签
        clean = System.Text.RegularExpressions.Regex.Replace(clean, @"<[^>]+>", " ");

        // 3. 移除图片语法
        clean = System.Text.RegularExpressions.Regex.Replace(clean, @"!\[.*?\]\(.*?\)", " ");

        // 4. 将链接保留为纯文字 [text](url) -> text
        clean = System.Text.RegularExpressions.Regex.Replace(clean, @"\[(.*?)\]\(.*?\)", "$1");

        // 5. 移除行内代码块
        clean = System.Text.RegularExpressions.Regex.Replace(clean, @"`.*?`", " ");

        // 6. 移除 Markdown 常用结构符号
        clean = System.Text.RegularExpressions.Regex.Replace(clean, @"[#*>~_=\-|\[\]\(\)]", " ");

        // 7. 规范化压缩连续空白
        clean = System.Text.RegularExpressions.Regex.Replace(clean, @"\s+", " ").Trim();

        return clean.Length > 150 ? clean[..150] + "..." : clean;
    }

    /// <summary>
    /// 根据正文字数估算阅读时间（以中文 350 字/分钟计算）
    /// </summary>
    /// <param name="content">正文字符串</param>
    /// <returns>阅读分钟数</returns>
    public static int CalculateReadingTime(string content)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            return 1;
        }

        var minutes = (int)Math.Ceiling(content.Length / 350.0);
        return Math.Max(1, minutes);
    }

    /// <summary>
    /// 添加到系列
    /// </summary>
    /// <param name="seriesId">系列ID</param>
    public void AddToSeries(long seriesId)
    {
        SeriesId = seriesId;
    }

    /// <summary>
    /// 提交管理员审核
    /// </summary>
    public void SubmitForReview()
    {
        Status = ArticleStatus.PendingReview;
        SetUpdatedAt();
    }

    /// <summary>
    /// 审核通过并正式发布
    /// </summary>
    public void Approve()
    {
        Status = ArticleStatus.Published;
        SetUpdatedAt();
    }

    /// <summary>
    /// 驳回/退回文章
    /// </summary>
    public void Reject()
    {
        Status = ArticleStatus.Rejected;
        SetUpdatedAt();
    }

    /// <summary>
    /// 保存为草稿
    /// </summary>
    public void SetAsDraft()
    {
        Status = ArticleStatus.Draft;
        SetUpdatedAt();
    }
}
