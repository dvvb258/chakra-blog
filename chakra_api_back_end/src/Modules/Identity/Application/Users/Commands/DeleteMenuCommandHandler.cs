using Identity.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using SharedKernel.Application.CQRS;
using SharedKernel.Application.Exceptions;

namespace Identity.Application.Users.Commands;

/// <summary>
/// 删除系统菜单命令处理器
/// </summary>
public sealed class DeleteMenuCommandHandler : ICommandHandler<DeleteMenuCommand>
{
    private readonly IdentityDbContext _dbContext;

    public DeleteMenuCommandHandler(IdentityDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task Handle(DeleteMenuCommand command, CancellationToken cancellationToken)
    {
        var menu = await _dbContext.Menus
            .FirstOrDefaultAsync(m => m.Id == command.Id && !m.IsDeleted, cancellationToken);

        if (menu == null)
        {
            throw new NotFoundException("Menu", command.Id);
        }

        // 检查是否有下级子菜单
        var hasChildren = await _dbContext.Menus
            .AnyAsync(m => m.ParentId == menu.Id && !m.IsDeleted, cancellationToken);

        if (hasChildren)
        {
            throw new BusinessException($"菜单 [{menu.Name}] 下存在子菜单项，请先删除或移除子菜单后再删除父级菜单", 400);
        }

        // 移除或软删除角色-菜单关联
        var relatedRoleMenus = await _dbContext.RoleMenus
            .Where(rm => rm.MenuId == menu.Id && !rm.IsDeleted)
            .ToListAsync(cancellationToken);

        _dbContext.RoleMenus.RemoveRange(relatedRoleMenus);

        // 软删除菜单
        menu.MarkAsDeleted();

        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
