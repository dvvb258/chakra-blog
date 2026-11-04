using Content.Domain.Entities;
using Content.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using SharedKernel.Application.CQRS;
using SharedKernel.Application.Exceptions;

namespace Content.Application.Categories.Commands;

/// <summary>
/// 创建文章分类命令处理器
/// </summary>
public sealed class CreateCategoryCommandHandler : ICommandHandler<CreateCategoryCommand, long>
{
    private readonly BlogDbContext _dbContext;

    public CreateCategoryCommandHandler(BlogDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<long> Handle(CreateCategoryCommand command, CancellationToken cancellationToken)
    {
        var req = command.Request;

        var name = req.Name.Trim();

        // 校验分类名称是否已存在
        var exists = await _dbContext.Categories
            .AnyAsync(c => c.Name.ToLower() == name.ToLower() && !c.IsDeleted, cancellationToken);

        if (exists)
        {
            throw new BusinessException($"分类名称 \"{name}\" 已存在，请更换名称", 400);
        }

        var category = new Category(
            name: name,
            description: req.Description?.Trim() ?? string.Empty,
            icon: req.Icon?.Trim() ?? string.Empty,
            sort: req.Sort);

        _dbContext.Categories.Add(category);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return category.Id;
    }
}
