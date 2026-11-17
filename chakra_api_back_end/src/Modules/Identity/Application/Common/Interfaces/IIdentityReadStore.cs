using Identity.Application.Common.Models;

namespace Identity.Application.Common.Interfaces;

/// <summary>
/// Identity 模块读模型存储（QueryHandler 与授权读路径使用 Dapper，EF Core 负责领域写入）
/// </summary>
public interface IIdentityReadStore
{
    /// <summary>
    /// 判断角色是否拥有指定权限代号
    /// </summary>
    /// <param name="roleCode">已规范化为小写的角色代号</param>
    /// <param name="permissionCode">已规范化为小写的权限代号</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>是否拥有该权限</returns>
    Task<bool> HasPermissionAsync(string roleCode, string permissionCode, CancellationToken cancellationToken = default);

    /// <summary>
    /// 判断用户是否存在、启用且角色与令牌声明一致
    /// </summary>
    /// <param name="userId">用户 ID</param>
    /// <param name="roleCode">已规范化为小写的角色代号</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>是否允许继续使用当前访问令牌</returns>
    Task<bool> IsAccessAllowedAsync(long userId, string roleCode, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取未删除用户的角色代号
    /// </summary>
    /// <param name="userId">用户 ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>角色代号；用户不存在时返回 null</returns>
    Task<string?> GetActiveUserRoleAsync(long userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取全量未删除菜单，按排序权重升序
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>菜单只读行集合</returns>
    Task<IReadOnlyList<MenuRow>> GetActiveMenusAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取指定角色已授权的未删除菜单，按排序权重升序
    /// </summary>
    /// <param name="roleCode">角色代号（大小写不敏感）</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>菜单只读行集合；角色不存在时返回空集合</returns>
    Task<IReadOnlyList<MenuRow>> GetMenusByRoleCodeAsync(string roleCode, CancellationToken cancellationToken = default);
}
