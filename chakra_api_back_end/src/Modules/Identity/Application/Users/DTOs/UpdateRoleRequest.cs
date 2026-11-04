namespace Identity.Application.Users.DTOs;

/// <summary>
/// 更新角色请求参数
/// </summary>
public sealed record UpdateRoleRequest
{
    /// <summary>
    /// 角色显示名称
    /// </summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>
    /// 角色职能描述
    /// </summary>
    public string? Description { get; init; }

    /// <summary>
    /// 关联授权的菜单ID列表
    /// </summary>
    public List<long>? MenuIds { get; init; }
}
