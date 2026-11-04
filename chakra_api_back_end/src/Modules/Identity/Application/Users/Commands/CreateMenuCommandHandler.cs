using Identity.Application.Users.DTOs;
using Identity.Domain.Entities;
using Identity.Domain.Enums;
using Identity.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using SharedKernel.Application.CQRS;
using SharedKernel.Application.Exceptions;
using SharedKernel.Constants;

namespace Identity.Application.Users.Commands;

/// <summary>
/// 创建系统菜单命令处理器
/// </summary>
public sealed class CreateMenuCommandHandler : ICommandHandler<CreateMenuCommand, MenuDto>
{
    private readonly IdentityDbContext _dbContext;

    public CreateMenuCommandHandler(IdentityDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<MenuDto> Handle(CreateMenuCommand command, CancellationToken cancellationToken)
    {
        var req = command.Request;

        if (string.IsNullOrWhiteSpace(req.Name))
        {
            throw new BusinessException("菜单名称不能为空", 400);
        }

        var menuType = req.Type.HasValue && Enum.IsDefined(typeof(MenuType), req.Type.Value)
            ? (MenuType)req.Type.Value
            : MenuType.Menu;

        if (menuType != MenuType.Button && string.IsNullOrWhiteSpace(req.Path))
        {
            throw new BusinessException("路由路径不能为空", 400);
        }

        // 验证父级菜单是否存在
        if (req.ParentId.HasValue && req.ParentId.Value > 0)
        {
            var parentExists = await _dbContext.Menus
                .AnyAsync(m => m.Id == req.ParentId.Value && !m.IsDeleted, cancellationToken);

            if (!parentExists)
            {
                throw new BusinessException("指定的父级菜单不存在或已被删除", 400);
            }
        }

        var permCode = string.IsNullOrWhiteSpace(req.PermissionCode)
            ? req.Path.Trim().TrimStart('/').Replace('/', ':')
            : req.PermissionCode.Trim();

        var menu = new Menu(
            name: req.Name.Trim(),
            path: req.Path?.Trim() ?? string.Empty,
            permissionCode: permCode,
            parentId: req.ParentId is > 0 ? req.ParentId : null,
            icon: req.Icon?.Trim(),
            sort: req.Sort,
            isHidden: menuType == MenuType.Button || req.IsHidden,
            type: menuType);

        _dbContext.Menus.Add(menu);

        // 为系统超管角色（Admin）自动预分配该菜单
        var adminRole = await _dbContext.Roles
            .FirstOrDefaultAsync(r => r.Code.ToLower() == RoleNames.Admin.ToLower() && !r.IsDeleted, cancellationToken);

        if (adminRole != null)
        {
            _dbContext.RoleMenus.Add(new RoleMenu(adminRole.Id, menu.Id));
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        return new MenuDto
        {
            Id = menu.Id,
            ParentId = menu.ParentId,
            Name = menu.Name,
            Path = menu.Path,
            PermissionCode = menu.PermissionCode,
            Icon = menu.Icon,
            Sort = menu.Sort,
            IsHidden = menu.IsHidden,
            Type = (int)menu.Type,
            Children = [],
        };
    }
}
