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

        // 方案 A 严格数据所有权隔离：只能修改自己创建的文章，管理员也不可擅自篡改他人草稿/文章
        if (article.CreatedByUserId != command.CurrentUserId)
        {
            throw new BusinessException("权限不足：您只能修改自己创建的文章内容", 403);
        }

        // 乐观并发锁核验：若客户端提供了版本号且与当前数据库不一致，阻断修改防覆盖
        if (req.Version.HasValue && req.Version.Value >= 0 && article.Version != req.Version.Value)
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

        // 方案 A 审核流规范：更新内容后，可选择【重新提交审核】或【保留为草稿】
        // 任何角色（包括管理员）修改正文均不可直接绕过审核流发布上线，发布必须通过审核确认
        if (req.Status == (int)ArticleStatus.PendingReview)
        {
            article.SubmitForReview();
        }
        else
        {
            article.SetAsDraft();
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
