using Identity.Application.Users.DTOs;
using Identity.Domain.Entities;
using Identity.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using SharedKernel.Application.CQRS;
using SharedKernel.Application.Exceptions;
using SharedKernel.Constants;

namespace Identity.Application.Users.Commands;

/// <summary>
/// 更新角色及其菜单权限处理器
/// </summary>
public sealed class UpdateRoleCommandHandler : ICommandHandler<UpdateRoleCommand, RoleDto>
{
    private readonly IdentityDbContext _dbContext;

    public UpdateRoleCommandHandler(IdentityDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<RoleDto> Handle(UpdateRoleCommand command, CancellationToken cancellationToken)
    {
        var role = await _dbContext.Roles
            .FirstOrDefaultAsync(r => r.Id == command.Id && !r.IsDeleted, cancellationToken);

        if (role == null)
        {
            throw new NotFoundException("Role", command.Id);
        }

        var req = command.Request;

        // 防死锁机制：系统核心管理员角色受保护
        if (string.Equals(role.Code, RoleNames.Admin, StringComparison.OrdinalIgnoreCase))
        {
            // Admin 只允许更新名称与描述，菜单权限永远锁定为全部，严禁剥夺
            role.Update(req.Name.Trim(), req.Description?.Trim() ?? string.Empty);
            await _dbContext.SaveChangesAsync(cancellationToken);

            var allMenuIds = await _dbContext.Menus
                .Where(m => !m.IsDeleted)
                .Select(m => m.Id)
                .ToListAsync(cancellationToken);

            return new RoleDto
            {
                Id = role.Id,
                Code = role.Code,
                Name = role.Name,
                Description = role.Description,
                IsSystem = true,
                MenuIds = allMenuIds,
            };
        }

        role.Update(req.Name.Trim(), req.Description?.Trim() ?? string.Empty);

        // 更新菜单权限关联：先删除既有关联，再重新写入
        var oldRoleMenus = await _dbContext.RoleMenus
            .Where(rm => rm.RoleId == role.Id)
            .ToListAsync(cancellationToken);

        _dbContext.RoleMenus.RemoveRange(oldRoleMenus);

        var assignedMenuIds = new List<long>();
        if (req.MenuIds is { Count: > 0 })
        {
            var validMenuIds = await _dbContext.Menus
                .Where(m => req.MenuIds.Contains(m.Id) && !m.IsDeleted)
                .Select(m => m.Id)
                .ToListAsync(cancellationToken);

            foreach (var menuId in validMenuIds)
            {
                _dbContext.RoleMenus.Add(new RoleMenu(role.Id, menuId));
            }

            assignedMenuIds = validMenuIds;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        return new RoleDto
        {
            Id = role.Id,
            Code = role.Code,
            Name = role.Name,
            Description = role.Description,
            IsSystem = role.IsSystem,
            MenuIds = assignedMenuIds,
        };
    }
}
