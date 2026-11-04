using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using SharedKernel.Application.Interfaces;
using SharedKernel.Constants;

namespace ApiHost.Authorization;

/// <summary>
/// 声明式系统权限代码策略授权评估处理器
/// </summary>
public sealed class PermissionAuthorizationHandler : AuthorizationHandler<PermissionRequirement>
{
    private readonly IPermissionChecker _permissionChecker;

    /// <summary>
    /// Initializes a new instance of the <see cref="PermissionAuthorizationHandler"/> class.
    /// </summary>
    /// <param name="permissionChecker">权限校验器</param>
    public PermissionAuthorizationHandler(IPermissionChecker permissionChecker)
    {
        _permissionChecker = permissionChecker;
    }

    /// <inheritdoc />
    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        PermissionRequirement requirement)
    {
        if (context.User.Identity?.IsAuthenticated != true)
        {
            return;
        }

        // 提取用户所属角色 Claim（JwtTokenService 生成时存放于 ClaimTypes.Role）
        var role = context.User.FindFirstValue(ClaimTypes.Role);
        if (string.IsNullOrWhiteSpace(role))
        {
            return;
        }

        // 1. 超级管理员拥有全量特权（Bypass），直接放行
        if (string.Equals(role, RoleNames.Admin, StringComparison.OrdinalIgnoreCase))
        {
            context.Succeed(requirement);
            return;
        }

        // 2. 普通角色：动态查询该角色被赋予的权限代号集合（OR 判定：具备其中任一权限即可通行）
        foreach (var perm in requirement.Permissions)
        {
            if (await _permissionChecker.HasPermissionAsync(role, perm))
            {
                context.Succeed(requirement);
                return;
            }
        }
    }
}
