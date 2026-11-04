using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;

namespace ApiHost.Authorization;

/// <summary>
/// 动态权限策略提供器，无需手动预先静态注册每个 permission policy
/// </summary>
public sealed class PermissionPolicyProvider : DefaultAuthorizationPolicyProvider
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PermissionPolicyProvider"/> class.
    /// </summary>
    /// <param name="options">授权配置项</param>
    public PermissionPolicyProvider(IOptions<AuthorizationOptions> options)
        : base(options)
    {
    }

    /// <inheritdoc />
    public override async Task<AuthorizationPolicy?> GetPolicyAsync(string policyName)
    {
        var existingPolicy = await base.GetPolicyAsync(policyName);
        if (existingPolicy != null)
        {
            return existingPolicy;
        }

        var permissions = policyName.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        // 针对未显式静态注册的策略名称，动态按 PermissionRequirement 构建鉴权策略
        return new AuthorizationPolicyBuilder()
            .RequireAuthenticatedUser()
            .AddRequirements(new PermissionRequirement(permissions))
            .Build();
    }
}
