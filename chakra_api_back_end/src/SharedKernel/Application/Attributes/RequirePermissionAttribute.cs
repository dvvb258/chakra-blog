using Microsoft.AspNetCore.Authorization;

namespace SharedKernel.Application.Attributes;

/// <summary>
/// 声明式细粒度策略鉴权特性（按系统权限代号动态校验，支持多权限 OR 判定）
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
public sealed class RequirePermissionAttribute : AuthorizeAttribute
{
    /// <summary>
    /// 目标权限代码列表（任一满足即判定授权成功）
    /// </summary>
    public string[] Permissions { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="RequirePermissionAttribute"/> class.
    /// </summary>
    /// <param name="permissions">系统权限代码（支持传入多个，任一满足即放行）</param>
    public RequirePermissionAttribute(params string[] permissions)
        : base(policy: string.Join(",", permissions))
    {
        Permissions = permissions;
    }
}
