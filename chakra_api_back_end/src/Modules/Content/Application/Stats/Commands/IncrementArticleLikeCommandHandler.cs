using Content.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using SharedKernel.Application.CQRS;
using SharedKernel.Application.Exceptions;

namespace Content.Application.Stats.Commands;

/// <summary>
/// 文章点赞数 +1 命令处理器
/// </summary>
public sealed class IncrementArticleLikeCommandHandler : ICommandHandler<IncrementArticleLikeCommand>
{
    private readonly BlogDbContext _dbContext;
    private readonly IMemoryCache _cache;

    /// <summary>
    /// Initializes a new instance of the <see cref="IncrementArticleLikeCommandHandler"/> class.
    /// </summary>
    /// <param name="dbContext">数据库上下文</param>
    /// <param name="cache">内存缓存</param>
    public IncrementArticleLikeCommandHandler(BlogDbContext dbContext, IMemoryCache cache)
    {
        _dbContext = dbContext;
        _cache = cache;
    }

    /// <inheritdoc />
    public async Task Handle(IncrementArticleLikeCommand request, CancellationToken cancellationToken)
    {
        var clientKey = string.IsNullOrWhiteSpace(request.VisitorId)
            ? (string.IsNullOrWhiteSpace(request.ClientIp) ? "unknown" : request.ClientIp)
            : request.VisitorId;
        var cacheKey = $"article_like:{clientKey}:{request.ArticleId}";

        // 1. 服务端幂等与防刷去重：若缓存中已经记录该客户端的点赞状态，直接返回（防止重复调用 +1）
        if (_cache.TryGetValue(cacheKey, out bool isLiked) && isLiked)
        {
            return;
        }

        // 2. 更新点赞数：若为 InMemory 测试环境则回退为实体更新，生产环境采用极速原子单列递增
        if (_dbContext.Database.ProviderName == "Microsoft.EntityFrameworkCore.InMemory")
        {
            var article = await _dbContext.Articles.FirstOrDefaultAsync(a => a.Id == request.ArticleId && !a.IsDeleted, cancellationToken);
            if (article == null)
            {
                throw new NotFoundException("Article", request.ArticleId);
            }

            article.IncrementLikes();
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
        else
        {
            var rowsAffected = await _dbContext.Articles
                .Where(a => a.Id == request.ArticleId && !a.IsDeleted)
                .ExecuteUpdateAsync(
                    setters => setters.SetProperty(a => a.Likes, a => a.Likes + 1),
                    cancellationToken);

            if (rowsAffected == 0)
            {
                throw new NotFoundException("Article", request.ArticleId);
            }
        }

        // 3. 记录点赞缓存标记（保留 24 小时）
        _cache.Set(cacheKey, true, TimeSpan.FromHours(24));
    }
}
