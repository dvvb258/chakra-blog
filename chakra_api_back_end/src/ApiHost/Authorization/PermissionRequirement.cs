using Microsoft.AspNetCore.Authorization;

namespace ApiHost.Authorization;

/// <summary>
/// 基于系统权限代码的授权要求（支持多权限 OR 判定）
/// </summary>
public sealed class PermissionRequirement : IAuthorizationRequirement
{
    /// <summary>
    /// 目标系统权限代码列表
    /// </summary>
    public string[] Permissions { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="PermissionRequirement"/> class.
    /// </summary>
    /// <param name="permissions">权限代码列表</param>
    public PermissionRequirement(params string[] permissions)
    {
        Permissions = permissions;
    }
}
