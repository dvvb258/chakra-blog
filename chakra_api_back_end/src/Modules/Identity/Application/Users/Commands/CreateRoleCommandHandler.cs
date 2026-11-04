using Identity.Application.Users.DTOs;
using Identity.Domain.Entities;
using Identity.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using SharedKernel.Application.CQRS;
using SharedKernel.Application.Exceptions;

namespace Identity.Application.Users.Commands;

/// <summary>
/// 创建角色命令处理器
/// </summary>
public sealed class CreateRoleCommandHandler : ICommandHandler<CreateRoleCommand, RoleDto>
{
    private readonly IdentityDbContext _dbContext;

    public CreateRoleCommandHandler(IdentityDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<RoleDto> Handle(CreateRoleCommand command, CancellationToken cancellationToken)
    {
        var req = command.Request;
        var code = req.Code.Trim();

        // 检查角色代码是否冲突
        var exists = await _dbContext.Roles
            .AnyAsync(r => r.Code.ToLower() == code.ToLower(), cancellationToken);

        if (exists)
        {
            throw new BusinessException($"角色代号 [{code}] 已存在，请使用其他代号", 400);
        }

        var role = new Role(
            code: code,
            name: req.Name.Trim(),
            description: req.Description?.Trim() ?? string.Empty,
            isSystem: false);

        _dbContext.Roles.Add(role);

        // 绑定勾选的菜单
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
