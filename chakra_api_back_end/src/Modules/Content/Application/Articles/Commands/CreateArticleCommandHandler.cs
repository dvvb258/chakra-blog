using Content.Domain.Entities;
using Content.Domain.Enums;
using Content.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using SharedKernel.Application.CQRS;
using SharedKernel.Application.Exceptions;

namespace Content.Application.Articles.Commands;

/// <summary>
/// 创建文章命令处理器
/// </summary>
public sealed class CreateArticleCommandHandler : ICommandHandler<CreateArticleCommand, long>
{
    private readonly BlogDbContext _dbContext;

    /// <summary>
    /// Initializes a new instance of the <see cref="CreateArticleCommandHandler"/> class.
    /// </summary>
    /// <param name="dbContext">数据库上下文</param>
    public CreateArticleCommandHandler(BlogDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    /// <inheritdoc />
    public async Task<long> Handle(CreateArticleCommand command, CancellationToken cancellationToken)
    {
        var req = command.Request;

        var author = string.IsNullOrWhiteSpace(req.Author)
            ? (string.IsNullOrWhiteSpace(command.CurrentUserNickname) ? "博主" : command.CurrentUserNickname.Trim())
            : req.Author.Trim();

        var summary = string.IsNullOrWhiteSpace(req.Summary)
            ? Article.ExtractSummary(req.Content)
            : req.Summary.Trim();

        var readingTime = req.ReadingTime > 0
            ? req.ReadingTime
            : Article.CalculateReadingTime(req.Content);

        // 校验所属分类有效性
        if (req.CategoryId <= 0)
        {
            throw new BusinessException("请指定有效的文章所属分类", 400);
        }

        var categoryEntity = await _dbContext.Categories
            .FirstOrDefaultAsync(c => c.Id == req.CategoryId && !c.IsDeleted, cancellationToken);

        if (categoryEntity == null)
        {
            throw new BusinessException($"指定的分类ID [{req.CategoryId}] 不存在或已被删除", 400);
        }

        // 文章发布审核流：
        // 管理员新建文章且指定 Published 时，允许免审直接发布上线；
        // 非管理员角色新建文章，初始状态根据选择进入【草稿 (Draft)】或【提交审核 (PendingReview)】
        var initialStatus = (command.IsAdmin && req.Status == (int)ArticleStatus.Published)
            ? ArticleStatus.Published
            : (req.Status == (int)ArticleStatus.PendingReview ? ArticleStatus.PendingReview : ArticleStatus.Draft);

        var article = new Article(
            title: req.Title.Trim(),
            content: req.Content,
            summary: summary,
            tags: req.Tags ?? [],
            categoryId: categoryEntity.Id,
            author: author,
            readingTime: readingTime,
            coverColor: req.CoverColor ?? string.Empty,
            coverUrl: req.CoverUrl ?? string.Empty,
            createdByUserId: command.CurrentUserId,
            status: initialStatus);

        if (req.SeriesId.HasValue && req.SeriesId.Value > 0)
        {
            var seriesExists = await _dbContext.Series
                .AnyAsync(s => s.Id == req.SeriesId.Value && !s.IsDeleted, cancellationToken);
            if (!seriesExists)
            {
                throw new BusinessException($"指定的专栏ID [{req.SeriesId.Value}] 不存在或已被删除", 400);
            }

            article.AddToSeries(req.SeriesId.Value);
        }

        _dbContext.Articles.Add(article);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return article.Id;
    }
}
