using Content.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using SharedKernel.Application.CQRS;
using SharedKernel.Application.Exceptions;

namespace Content.Application.Stats.Commands;

/// <summary>
/// 文章阅读量 +1 命令处理器
/// </summary>
public sealed class IncrementArticleViewCommandHandler : ICommandHandler<IncrementArticleViewCommand>
{
    private readonly BlogDbContext _dbContext;

    /// <summary>
    /// Initializes a new instance of the <see cref="IncrementArticleViewCommandHandler"/> class.
    /// </summary>
    /// <param name="dbContext">数据库上下文</param>
    public IncrementArticleViewCommandHandler(BlogDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    /// <inheritdoc />
    public async Task Handle(IncrementArticleViewCommand request, CancellationToken cancellationToken)
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
}
