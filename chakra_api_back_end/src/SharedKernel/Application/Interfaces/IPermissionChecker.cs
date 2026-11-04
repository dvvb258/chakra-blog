namespace SharedKernel.Application.Interfaces;

/// <summary>
/// 角色权限验证服务契约
/// </summary>
public interface IPermissionChecker
{
    /// <summary>
    /// 判断指定角色是否具备指定权限代码
    /// </summary>
    /// <param name="roleCode">角色代号</param>
    /// <param name="permissionCode">权限资源代号（如 article:manage）</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>是否具备权限</returns>
    Task<bool> HasPermissionAsync(string roleCode, string permissionCode, CancellationToken cancellationToken = default);
}
