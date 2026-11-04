using Content.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using SharedKernel.Application.CQRS;
using SharedKernel.Application.Exceptions;

namespace Content.Application.Categories.Commands;

/// <summary>
/// 更新文章分类命令处理器
/// </summary>
public sealed class UpdateCategoryCommandHandler : ICommandHandler<UpdateCategoryCommand>
{
    private readonly BlogDbContext _dbContext;

    public UpdateCategoryCommandHandler(BlogDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task Handle(UpdateCategoryCommand command, CancellationToken cancellationToken)
    {
        var category = await _dbContext.Categories
            .FirstOrDefaultAsync(c => c.Id == command.Id, cancellationToken);

        if (category == null)
        {
            throw new NotFoundException("Category", command.Id);
        }

        var req = command.Request;
        var name = req.Name.Trim();

        // 校验变更后的分类名称是否被其他分类占用
        var duplicate = await _dbContext.Categories
            .AnyAsync(c => c.Name.ToLower() == name.ToLower() && c.Id != command.Id && !c.IsDeleted, cancellationToken);

        if (duplicate)
        {
            throw new BusinessException($"分类名称 \"{name}\" 已被其他分类使用", 400);
        }

        category.Update(
            name: name,
            description: req.Description?.Trim() ?? string.Empty,
            icon: req.Icon?.Trim() ?? string.Empty,
            sort: req.Sort);

        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
