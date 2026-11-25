using Content.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using SharedKernel.Application.CQRS;
using SharedKernel.Application.Exceptions;

namespace Content.Application.Stats.Commands;

/// <summary>
/// 文章阅读量 +1 命令处理器
/// </summary>
public sealed class IncrementArticleViewCommandHandler : ICommandHandler<IncrementArticleViewCommand>
{
    private static readonly TimeSpan ViewDeduplicationWindow = TimeSpan.FromMinutes(30);

    private readonly BlogDbContext _dbContext;
    private readonly IMemoryCache _cache;

    /// <summary>
    /// Initializes a new instance of the <see cref="IncrementArticleViewCommandHandler"/> class.
    /// </summary>
    /// <param name="dbContext">数据库上下文</param>
    /// <param name="cache">内存缓存</param>
    public IncrementArticleViewCommandHandler(BlogDbContext dbContext, IMemoryCache cache)
    {
        _dbContext = dbContext;
        _cache = cache;
    }

    /// <inheritdoc />
    public async Task Handle(IncrementArticleViewCommand request, CancellationToken cancellationToken)
    {
        var visitorKey = VisitorIdentity.Resolve(request.VisitorId, request.ClientIp);
        var cacheKey = $"article_view:{request.ArticleId}:{visitorKey}";

        if (_cache.TryGetValue(cacheKey, out _))
        {
            return;
        }

        if (_dbContext.Database.ProviderName == "Microsoft.EntityFrameworkCore.InMemory")
        {
            var article = await _dbContext.Articles.FirstOrDefaultAsync(
                a => a.Id == request.ArticleId && !a.IsDeleted,
                cancellationToken);
            if (article == null)
            {
                throw new NotFoundException("Article", request.ArticleId);
            }

            article.IncrementViews();
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
        else
        {
            // 原子单列递增：避免将几十 KB 正文加载到内存，避免 ChangeTracker 开销，且不污染 updated_at
            var rowsAffected = await _dbContext.Articles
                .Where(a => a.Id == request.ArticleId && !a.IsDeleted)
                .ExecuteUpdateAsync(
                    setters => setters.SetProperty(a => a.Views, a => a.Views + 1),
                    cancellationToken);

            if (rowsAffected == 0)
            {
                throw new NotFoundException("Article", request.ArticleId);
            }
        }

        _cache.Set(cacheKey, true, ViewDeduplicationWindow);
    }
}
