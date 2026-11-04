using SharedKernel.Constants;

namespace Identity.Application.Users.DTOs;

/// <summary>
/// 修改用户角色请求体
/// </summary>
public sealed record UpdateUserRoleRequest
{
    /// <summary>目标角色名称（Admin 或 Editor）</summary>
    public string Role { get; init; } = RoleNames.Editor;
}
