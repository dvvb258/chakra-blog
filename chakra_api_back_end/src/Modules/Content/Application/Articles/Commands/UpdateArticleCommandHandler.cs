using Content.Domain.Enums;
using Content.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using SharedKernel.Application.CQRS;
using SharedKernel.Application.Exceptions;

namespace Content.Application.Articles.Commands;

/// <summary>
/// 更新文章命令处理器
/// </summary>
public sealed class UpdateArticleCommandHandler : ICommandHandler<UpdateArticleCommand>
{
    private readonly BlogDbContext _dbContext;

    /// <summary>
    /// Initializes a new instance of the <see cref="UpdateArticleCommandHandler"/> class.
    /// </summary>
    /// <param name="dbContext">数据库上下文</param>
    public UpdateArticleCommandHandler(BlogDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    /// <inheritdoc />
    public async Task Handle(UpdateArticleCommand command, CancellationToken cancellationToken)
    {
        var req = command.Request;

        var article = await _dbContext.Articles
            .FirstOrDefaultAsync(a => a.Id == command.Id, cancellationToken);

        if (article == null)
        {
            throw new NotFoundException("Article", command.Id);
        }

        // 权限校验：非管理员只能修改自己名下的文章
        if (!command.IsAdmin && article.CreatedByUserId != command.CurrentUserId)
        {
            throw new BusinessException("权限不足：您只能修改自己创建的文章内容", 403);
        }

        // 乐观并发锁核验：若客户端提供了版本号且与当前数据库不一致，阻断修改防覆盖
        if (req.Version.HasValue && article.Version != req.Version.Value)
        {
            throw new BusinessException(
                $"当前文章已被其他页面或用户更新（数据库当前: v{article.Version}，提交版本: v{req.Version.Value}）。为防止覆盖他人内容，请刷新页面加载最新数据后再编辑。",
                409);
        }

        var seriesId = req.SeriesId.HasValue && req.SeriesId.Value > 0
            ? req.SeriesId.Value
            : (long?)null;

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

        if (seriesId.HasValue)
        {
            var seriesExists = await _dbContext.Series
                .AnyAsync(s => s.Id == seriesId.Value && !s.IsDeleted, cancellationToken);
            if (!seriesExists)
            {
                throw new BusinessException($"指定的专栏ID [{seriesId.Value}] 不存在或已被删除", 400);
            }
        }

        article.Update(
            title: req.Title,
            content: req.Content,
            summary: req.Summary ?? string.Empty,
            tags: req.Tags,
            categoryId: categoryEntity.Id,
            author: req.Author ?? string.Empty,
            readingTime: req.ReadingTime,
            coverColor: req.CoverColor ?? string.Empty,
            coverUrl: req.CoverUrl,
            seriesId: seriesId);

        // 文章状态流转规范：
        // 1. 若显式指定为【提交审核】(PendingReview) -> 进入待审
        // 2. 若显式指定为【下线为草稿】(Draft) -> 设为草稿
        // 3. 若为管理员操作且指定发布 (Published) 或文章原先就是 Published -> 允许保持/发布为 Published
        // 4. 普通作者修改已发布文章且未显式提审 -> 降为草稿待修改
        if (req.Status == (int)ArticleStatus.PendingReview)
        {
            article.SubmitForReview();
        }
        else if (req.Status == (int)ArticleStatus.Draft)
        {
            article.SetAsDraft();
        }
        else if (command.IsAdmin && (req.Status == (int)ArticleStatus.Published || article.Status == ArticleStatus.Published))
        {
            article.Approve();
        }
        else if (article.Status == ArticleStatus.Published)
        {
            article.SetAsDraft();
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
