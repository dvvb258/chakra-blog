using Content.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using SharedKernel.Application.CQRS;
using SharedKernel.Application.Exceptions;

namespace Content.Application.Articles.Commands;

/// <summary>
/// 软删除文章命令处理器
/// </summary>
public sealed class DeleteArticleCommandHandler : ICommandHandler<DeleteArticleCommand>
{
    private readonly BlogDbContext _dbContext;

    /// <summary>
    /// Initializes a new instance of the <see cref="DeleteArticleCommandHandler"/> class.
    /// </summary>
    /// <param name="dbContext">数据库上下文</param>
    public DeleteArticleCommandHandler(BlogDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    /// <inheritdoc />
    public async Task Handle(DeleteArticleCommand command, CancellationToken cancellationToken)
    {
        var article = await _dbContext.Articles
            .FirstOrDefaultAsync(a => a.Id == command.Id, cancellationToken);

        if (article == null)
        {
            throw new NotFoundException("Article", command.Id);
        }

        // 权限校验：非管理员只能删除自己名下的文章
        if (!command.IsAdmin && article.CreatedByUserId != command.CurrentUserId)
        {
            throw new BusinessException("权限不足：您只能删除自己创建的文章", 403);
        }

        // 仅执行软删除：保留封面与正文引用的媒体文件，便于误删排查与后续人工回收。
        // 如需自动清理，请通过独立的后台任务在确认无引用后删除，避免误删被其他文章复用的文件。
        article.MarkAsDeleted();
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
