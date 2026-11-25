using Content.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using SharedKernel.Application.CQRS;
using SharedKernel.Application.Exceptions;

namespace Content.Application.Stats.Commands;

/// <summary>
/// 文章取消点赞命令处理器
/// </summary>
public sealed class DecrementArticleLikeCommandHandler : ICommandHandler<DecrementArticleLikeCommand>
{
    private readonly BlogDbContext _dbContext;

    /// <summary>
    /// Initializes a new instance of the <see cref="DecrementArticleLikeCommandHandler"/> class.
    /// </summary>
    /// <param name="dbContext">数据库上下文</param>
    public DecrementArticleLikeCommandHandler(BlogDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    /// <inheritdoc />
    public async Task Handle(DecrementArticleLikeCommand request, CancellationToken cancellationToken)
    {
        var visitorKey = VisitorIdentity.Resolve(request.VisitorId, request.ClientIp);

        // 1. 没有持久化点赞关系时直接返回，禁止无记录直接扣减文章点赞数
        if (_dbContext.Database.ProviderName == "Microsoft.EntityFrameworkCore.InMemory")
        {
            var like = await _dbContext.ArticleLikes.FirstOrDefaultAsync(
                item => item.ArticleId == request.ArticleId && item.VisitorId == visitorKey,
                cancellationToken);
            if (like == null)
            {
                return;
            }

            var article = await _dbContext.Articles.FirstOrDefaultAsync(
                a => a.Id == request.ArticleId && !a.IsDeleted,
                cancellationToken);
            if (article == null)
            {
                throw new NotFoundException("Article", request.ArticleId);
            }

            _dbContext.ArticleLikes.Remove(like);
            article.DecrementLikes();
            await _dbContext.SaveChangesAsync(cancellationToken);
            return;
        }

        await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
        var deletedRows = await _dbContext.ArticleLikes
            .Where(like => like.ArticleId == request.ArticleId && like.VisitorId == visitorKey)
            .ExecuteDeleteAsync(cancellationToken);
        if (deletedRows == 0)
        {
            return;
        }

        // 2. 原子扣减并保底非负
        var rowsAffected = await _dbContext.Articles
            .Where(a => a.Id == request.ArticleId && !a.IsDeleted)
            .ExecuteUpdateAsync(
                setters => setters.SetProperty(a => a.Likes, a => a.Likes > 0 ? a.Likes - 1 : 0),
                cancellationToken);

        if (rowsAffected == 0)
        {
            throw new NotFoundException("Article", request.ArticleId);
        }

        await transaction.CommitAsync(cancellationToken);
    }
}
