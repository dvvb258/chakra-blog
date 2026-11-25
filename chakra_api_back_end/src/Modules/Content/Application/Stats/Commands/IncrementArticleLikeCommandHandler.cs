using Content.Domain.Entities;
using Content.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using SharedKernel.Application.CQRS;
using SharedKernel.Application.Exceptions;

namespace Content.Application.Stats.Commands;

/// <summary>
/// 文章点赞数 +1 命令处理器
/// </summary>
public sealed class IncrementArticleLikeCommandHandler : ICommandHandler<IncrementArticleLikeCommand>
{
    private readonly BlogDbContext _dbContext;

    /// <summary>
    /// Initializes a new instance of the <see cref="IncrementArticleLikeCommandHandler"/> class.
    /// </summary>
    /// <param name="dbContext">数据库上下文</param>
    public IncrementArticleLikeCommandHandler(BlogDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    /// <inheritdoc />
    public async Task Handle(IncrementArticleLikeCommand request, CancellationToken cancellationToken)
    {
        var visitorKey = VisitorIdentity.Resolve(request.VisitorId, request.ClientIp);

        // 1. 持久化幂等：同一访客对同一文章只允许一条点赞关系
        if (await _dbContext.ArticleLikes.AnyAsync(
                like => like.ArticleId == request.ArticleId && like.VisitorId == visitorKey,
                cancellationToken))
        {
            return;
        }

        // 2. 更新点赞关系与文章计数
        if (_dbContext.Database.ProviderName == "Microsoft.EntityFrameworkCore.InMemory")
        {
            var article = await _dbContext.Articles.FirstOrDefaultAsync(a => a.Id == request.ArticleId && !a.IsDeleted, cancellationToken);
            if (article == null)
            {
                throw new NotFoundException("Article", request.ArticleId);
            }

            article.IncrementLikes();
            _dbContext.ArticleLikes.Add(new ArticleLike(request.ArticleId, visitorKey));
            await _dbContext.SaveChangesAsync(cancellationToken);
            return;
        }

        var articleExists = await _dbContext.Articles
            .AnyAsync(a => a.Id == request.ArticleId && !a.IsDeleted, cancellationToken);
        if (!articleExists)
        {
            throw new NotFoundException("Article", request.ArticleId);
        }

        await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
        _dbContext.ArticleLikes.Add(new ArticleLike(request.ArticleId, visitorKey));

        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (IsDuplicateLike(exception))
        {
            return;
        }

        var rowsAffected = await _dbContext.Articles
            .Where(a => a.Id == request.ArticleId && !a.IsDeleted)
            .ExecuteUpdateAsync(
                setters => setters.SetProperty(a => a.Likes, a => a.Likes + 1),
                cancellationToken);

        if (rowsAffected == 0)
        {
            throw new NotFoundException("Article", request.ArticleId);
        }

        await transaction.CommitAsync(cancellationToken);
    }

    private static bool IsDuplicateLike(DbUpdateException exception)
    {
        return exception.InnerException is PostgresException
        {
            SqlState: PostgresErrorCodes.UniqueViolation,
        };
    }
}
