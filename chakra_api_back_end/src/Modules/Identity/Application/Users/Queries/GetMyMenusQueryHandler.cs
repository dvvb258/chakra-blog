using Identity.Application.Users.DTOs;
using Identity.Domain.Enums;
using Identity.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using SharedKernel.Application.CQRS;
using SharedKernel.Constants;

namespace Identity.Application.Users.Queries;

/// <summary>
/// 获取当前用户可访问菜单处理器
/// </summary>
public sealed class GetMyMenusQueryHandler : IQueryHandler<GetMyMenusQuery, List<MenuDto>>
{
    private readonly IdentityDbContext _dbContext;

    /// <summary>
    /// 初始化查询处理器
    /// </summary>
    /// <param name="dbContext">身份数据库上下文</param>
    public GetMyMenusQueryHandler(IdentityDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    /// <inheritdoc />
    public async Task<List<MenuDto>> Handle(GetMyMenusQuery request, CancellationToken cancellationToken)
    {
        var user = await _dbContext.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == request.UserId && !u.IsDeleted, cancellationToken);

        if (user == null)
        {
            return [];
        }

        // 核心法则：超级管理员拥有通配全量特权，零配置获取系统所有菜单
        if (string.Equals(user.Role, RoleNames.Admin, StringComparison.OrdinalIgnoreCase))
        {
            return await _dbContext.Menus
                .AsNoTracking()
                .Where(m => !m.IsDeleted && m.Type != MenuType.Button)
                .OrderBy(m => m.Sort)
                .Select(m => new MenuDto
                {
                    Id = m.Id,
                    ParentId = m.ParentId,
                    Name = m.Name,
                    Path = m.Path,
                    PermissionCode = m.PermissionCode,
                    Icon = m.Icon,
                    Sort = m.Sort,
                    IsHidden = m.IsHidden,
                    Type = (int)m.Type,
                })
                .ToListAsync(cancellationToken);
        }

        // 普通角色：根据角色与菜单映射关联表动态获取
        var role = await _dbContext.Roles
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.Code == user.Role && !r.IsDeleted, cancellationToken);

        if (role == null)
        {
            return [];
        }

        var authorizedMenuIds = await _dbContext.RoleMenus
            .AsNoTracking()
            .Where(rm => rm.RoleId == role.Id && !rm.IsDeleted)
            .Select(rm => rm.MenuId)
            .ToListAsync(cancellationToken);

        var menus = await _dbContext.Menus
            .AsNoTracking()
            .Where(m => authorizedMenuIds.Contains(m.Id) && !m.IsDeleted && m.Type != MenuType.Button)
            .OrderBy(m => m.Sort)
            .Select(m => new MenuDto
            {
                Id = m.Id,
                ParentId = m.ParentId,
                Name = m.Name,
                Path = m.Path,
                PermissionCode = m.PermissionCode,
                Icon = m.Icon,
                Sort = m.Sort,
                IsHidden = m.IsHidden,
                Type = (int)m.Type,
            })
            .ToListAsync(cancellationToken);

        return menus;
    }
}
