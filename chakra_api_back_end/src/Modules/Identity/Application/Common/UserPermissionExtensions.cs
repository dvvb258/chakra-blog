using Identity.Application.Common.Interfaces;
using Identity.Domain.Enums;
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
    /// <param name="readStore">Identity 读模型存储</param>
    /// <param name="roleCode">用户角色代号</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>菜单路径列表与权限标识列表</returns>
    public static async Task<(List<string> Menus, List<string> Permissions)> GetUserMenusAndPermissionsAsync(
        this IIdentityReadStore readStore,
        string roleCode,
        CancellationToken cancellationToken = default)
    {
        // 核心法则：超级管理员在底层天然拥有通配特权，自适应全量系统菜单
        var menus = string.Equals(roleCode, RoleNames.Admin, StringComparison.OrdinalIgnoreCase)
            ? await readStore.GetActiveMenusAsync(cancellationToken)
            : await readStore.GetMenusByRoleCodeAsync(roleCode, cancellationToken);

        return (
            menus.Where(m => m.Type != (int)MenuType.Button && !string.IsNullOrWhiteSpace(m.Path)).Select(m => m.Path).Distinct().ToList(),
            menus.Where(m => !string.IsNullOrWhiteSpace(m.PermissionCode)).Select(m => m.PermissionCode).Distinct().ToList());
    }
}
