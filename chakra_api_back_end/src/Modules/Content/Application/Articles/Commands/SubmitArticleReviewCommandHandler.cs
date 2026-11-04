using Content.Domain.Enums;
using Content.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using SharedKernel.Application.CQRS;
using SharedKernel.Application.Exceptions;

namespace Content.Application.Articles.Commands;

/// <summary>
/// 提交文章审核命令处理器
/// </summary>
public sealed class SubmitArticleReviewCommandHandler : ICommandHandler<SubmitArticleReviewCommand>
{
    private readonly BlogDbContext _dbContext;

    public SubmitArticleReviewCommandHandler(BlogDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task Handle(SubmitArticleReviewCommand command, CancellationToken cancellationToken)
    {
        var article = await _dbContext.Articles
            .FirstOrDefaultAsync(a => a.Id == command.Id, cancellationToken);

        if (article == null)
        {
            throw new NotFoundException("Article", command.Id);
        }

        // 方案 A 严格数据所有权隔离：只有文章作者本人可以提交自己的文章进行审核（管理员也不可代为提审他人草稿）
        if (article.CreatedByUserId != command.CurrentUserId)
        {
            throw new BusinessException("权限不足：您只能提交自己创建的文章进行审核", 403);
        }

        // 状态核验：仅草稿或驳回状态的文章允许提交审核
        if (article.Status != ArticleStatus.Draft && article.Status != ArticleStatus.Rejected)
        {
            throw new BusinessException("当前文章状态不支持提审操作", 400);
        }

        article.SubmitForReview();
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
