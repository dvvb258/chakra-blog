using Content.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
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
        // 原子单列递增：避免将正文加载到内存，避免 ChangeTracker 开销，且不污染 updated_at
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
}
