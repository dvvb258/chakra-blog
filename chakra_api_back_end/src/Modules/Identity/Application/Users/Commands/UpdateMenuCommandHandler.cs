using Identity.Application.Users.DTOs;
using Identity.Domain.Enums;
using Identity.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using SharedKernel.Application.CQRS;
using SharedKernel.Application.Exceptions;

namespace Identity.Application.Users.Commands;

/// <summary>
/// 更新系统菜单命令处理器
/// </summary>
public sealed class UpdateMenuCommandHandler : ICommandHandler<UpdateMenuCommand, MenuDto>
{
    private readonly IdentityDbContext _dbContext;

    public UpdateMenuCommandHandler(IdentityDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<MenuDto> Handle(UpdateMenuCommand command, CancellationToken cancellationToken)
    {
        var menu = await _dbContext.Menus
            .FirstOrDefaultAsync(m => m.Id == command.Id && !m.IsDeleted, cancellationToken);

        if (menu == null)
        {
            throw new NotFoundException("Menu", command.Id);
        }

        var req = command.Request;

        if (string.IsNullOrWhiteSpace(req.Name))
        {
            throw new BusinessException("菜单名称不能为空", 400);
        }

        var menuType = req.Type.HasValue && Enum.IsDefined(typeof(MenuType), req.Type.Value)
            ? (MenuType)req.Type.Value
            : menu.Type;

        if (menuType != MenuType.Button && string.IsNullOrWhiteSpace(req.Path))
        {
            throw new BusinessException("路由路径不能为空", 400);
        }

        // 避免死循环成环：父级不能是自己
        if (req.ParentId.HasValue && req.ParentId.Value == menu.Id)
        {
            throw new BusinessException("菜单的父级不能设置为自身", 400);
        }

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

        var parentId = req.ParentId is > 0 ? req.ParentId : null;

        menu.Update(
            name: req.Name.Trim(),
            path: req.Path?.Trim() ?? string.Empty,
            permissionCode: permCode,
            parentId: parentId,
            icon: req.Icon?.Trim(),
            sort: req.Sort,
            isHidden: menuType == MenuType.Button || req.IsHidden,
            type: menuType);

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
