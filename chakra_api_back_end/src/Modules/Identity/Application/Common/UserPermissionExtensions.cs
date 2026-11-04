using Identity.Domain.Enums;
using Identity.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using SharedKernel.Constants;

namespace Identity.Application.Common;

/// <summary>
/// 用户菜单与权限扩展方法
/// </summary>
public static class UserPermissionExtensions
{
    /// <summary>
    /// 根据用户角色动态解析授权的菜单路径和权限代码集合
    /// </summary>
    /// <param name="dbContext">数据库上下文</param>
    /// <param name="roleCode">用户角色代号</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>菜单路径列表与权限标识列表</returns>
    public static async Task<(List<string> Menus, List<string> Permissions)> GetUserMenusAndPermissionsAsync(
        this IdentityDbContext dbContext,
        string roleCode,
        CancellationToken cancellationToken = default)
    {
        // 核心法则：超级管理员在底层天然拥有通配特权，自适应全量系统菜单
        if (string.Equals(roleCode, RoleNames.Admin, StringComparison.OrdinalIgnoreCase))
        {
            var allMenus = await dbContext.Menus
                .AsNoTracking()
                .Where(m => !m.IsDeleted)
                .OrderBy(m => m.Sort)
                .ToListAsync(cancellationToken);

            return (
                allMenus.Where(m => m.Type != MenuType.Button && !string.IsNullOrWhiteSpace(m.Path)).Select(m => m.Path).Distinct().ToList(),
                allMenus.Select(m => m.PermissionCode).Distinct().ToList());
        }

        // 普通角色：根据角色与菜单映射关联表动态获取
        var role = await dbContext.Roles
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.Code == roleCode && !r.IsDeleted, cancellationToken);

        if (role == null)
        {
            return ([], []);
        }

        var authorizedMenuIds = await dbContext.RoleMenus
            .AsNoTracking()
            .Where(rm => rm.RoleId == role.Id && !rm.IsDeleted)
            .Select(rm => rm.MenuId)
            .ToListAsync(cancellationToken);

        var menus = await dbContext.Menus
            .AsNoTracking()
            .Where(m => authorizedMenuIds.Contains(m.Id) && !m.IsDeleted)
            .OrderBy(m => m.Sort)
            .ToListAsync(cancellationToken);

        // 查找所有包含子节点（菜单/按钮）的父级容器菜单 ID 集合
        // 此类容器节点的权限代码（如 category:manage, article:manage）仅供页面结构组织，绝不作为普通角色的细粒度操作权限下发
        var parentMenuIds = await dbContext.Menus
            .AsNoTracking()
            .Where(m => !m.IsDeleted && m.ParentId != null)
            .Select(m => m.ParentId!.Value)
            .Distinct()
            .ToListAsync(cancellationToken);

        var parentMenuIdSet = parentMenuIds.ToHashSet();

        return (
            menus.Where(m => m.Type != MenuType.Button && !string.IsNullOrWhiteSpace(m.Path)).Select(m => m.Path).Distinct().ToList(),
            menus.Where(m => !parentMenuIdSet.Contains(m.Id) && !string.IsNullOrWhiteSpace(m.PermissionCode)).Select(m => m.PermissionCode).Distinct().ToList());
    }
}
