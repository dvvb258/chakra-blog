using Content.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using SharedKernel.Application.CQRS;
using SharedKernel.Application.Exceptions;

namespace Content.Application.Stats.Commands;

/// <summary>
/// 文章取消点赞命令处理器
/// </summary>
public sealed class DecrementArticleLikeCommandHandler : ICommandHandler<DecrementArticleLikeCommand>
{
    private readonly BlogDbContext _dbContext;
    private readonly IMemoryCache _cache;

    /// <summary>
    /// Initializes a new instance of the <see cref="DecrementArticleLikeCommandHandler"/> class.
    /// </summary>
    /// <param name="dbContext">数据库上下文</param>
    /// <param name="cache">内存缓存</param>
    public DecrementArticleLikeCommandHandler(BlogDbContext dbContext, IMemoryCache cache)
    {
        _dbContext = dbContext;
        _cache = cache;
    }

    /// <inheritdoc />
    public async Task Handle(DecrementArticleLikeCommand request, CancellationToken cancellationToken)
    {
        var clientKey = string.IsNullOrWhiteSpace(request.VisitorId)
            ? (string.IsNullOrWhiteSpace(request.ClientIp) ? "unknown" : request.ClientIp)
            : request.VisitorId;
        var cacheKey = $"article_like:{clientKey}:{request.ArticleId}";

        // 1. 若客户端未曾点过赞（例如恶意直接刷 DELETE），返回幂等成功，不扣减数据库
        if (_cache.TryGetValue(cacheKey, out bool isLiked) && !isLiked)
        {
            return;
        }

        // 2. 扣减点赞数：若为 InMemory 测试环境则回退为实体更新，生产环境采用极速原子单列递减（保底非负）
        if (_dbContext.Database.ProviderName == "Microsoft.EntityFrameworkCore.InMemory")
        {
            var article = await _dbContext.Articles.FirstOrDefaultAsync(a => a.Id == request.ArticleId && !a.IsDeleted, cancellationToken);
            if (article == null)
            {
                throw new NotFoundException("Article", request.ArticleId);
            }

            article.DecrementLikes();
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
        else
        {
            var rowsAffected = await _dbContext.Articles
                .Where(a => a.Id == request.ArticleId && !a.IsDeleted)
                .ExecuteUpdateAsync(
                    setters => setters.SetProperty(a => a.Likes, a => a.Likes > 0 ? a.Likes - 1 : 0),
                    cancellationToken);

            if (rowsAffected == 0)
            {
                throw new NotFoundException("Article", request.ArticleId);
            }
        }

        // 3. 清除点赞状态缓存
        _cache.Remove(cacheKey);
    }
}
