using Content.Domain.Enums;
using Content.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using SharedKernel.Application.CQRS;
using SharedKernel.Application.Exceptions;

namespace Content.Application.Articles.Commands;

/// <summary>
/// 驳回文章审核命令处理器
/// </summary>
public sealed class RejectArticleCommandHandler : ICommandHandler<RejectArticleCommand>
{
    private readonly BlogDbContext _dbContext;

    public RejectArticleCommandHandler(BlogDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task Handle(RejectArticleCommand command, CancellationToken cancellationToken)
    {
        var article = await _dbContext.Articles
            .FirstOrDefaultAsync(a => a.Id == command.Id, cancellationToken);

        if (article == null)
        {
            throw new NotFoundException("Article", command.Id);
        }

        if (article.Status != ArticleStatus.PendingReview)
        {
            throw new BusinessException("只有处于待审核状态的文章才能进行驳回处理", 400);
        }

        article.Reject();
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
