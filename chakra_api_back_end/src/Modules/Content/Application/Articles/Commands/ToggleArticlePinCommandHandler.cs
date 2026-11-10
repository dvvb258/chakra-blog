using Content.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using SharedKernel.Application.CQRS;
using SharedKernel.Application.Exceptions;

namespace Content.Application.Articles.Commands;

/// <summary>
/// 切换文章置顶状态命令处理器
/// </summary>
public sealed class ToggleArticlePinCommandHandler : ICommandHandler<ToggleArticlePinCommand>
{
    private readonly BlogDbContext _dbContext;

    /// <summary>
    /// Initializes a new instance of the <see cref="ToggleArticlePinCommandHandler"/> class.
    /// </summary>
    /// <param name="dbContext">数据库上下文</param>
    public ToggleArticlePinCommandHandler(BlogDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    /// <inheritdoc />
    public async Task Handle(ToggleArticlePinCommand command, CancellationToken cancellationToken)
    {
        var article = await _dbContext.Articles
            .FirstOrDefaultAsync(a => a.Id == command.Id && !a.IsDeleted, cancellationToken);

        if (article == null)
        {
            throw new NotFoundException("Article", command.Id);
        }

        article.SetPinned(command.IsPinned);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
