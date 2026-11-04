using Content.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using SharedKernel.Application.CQRS;
using SharedKernel.Application.Exceptions;

namespace Content.Application.Categories.Commands;

/// <summary>
/// 软删除文章分类命令处理器
/// </summary>
public sealed class DeleteCategoryCommandHandler : ICommandHandler<DeleteCategoryCommand>
{
    private readonly BlogDbContext _dbContext;

    public DeleteCategoryCommandHandler(BlogDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task Handle(DeleteCategoryCommand command, CancellationToken cancellationToken)
    {
        var category = await _dbContext.Categories
            .FirstOrDefaultAsync(c => c.Id == command.Id, cancellationToken);

        if (category == null)
        {
            throw new NotFoundException("Category", command.Id);
        }

        // 保护策略：若该分类下仍有未删除文章，阻止删除
        var hasArticles = await _dbContext.Articles
            .AnyAsync(a => a.CategoryId == category.Id && !a.IsDeleted, cancellationToken);

        if (hasArticles)
        {
            throw new BusinessException($"分类 \"{category.Name}\" 下仍收录有文章，请先将相关文章移至其他分类后再删除", 400);
        }

        category.MarkAsDeleted();
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
