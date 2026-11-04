namespace Identity.Application.Users.DTOs;

/// <summary>
/// 创建角色请求参数
/// </summary>
public sealed record CreateRoleRequest
{
    /// <summary>
    /// 角色英文代号（如 Operator, Reviewer）
    /// </summary>
    public string Code { get; init; } = string.Empty;

    /// <summary>
    /// 角色名称（如 运营人员, 文章审核员）
    /// </summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>
    /// 角色描述
    /// </summary>
    public string? Description { get; init; }

    /// <summary>
    /// 勾选关联的菜单ID集合
    /// </summary>
    public List<long>? MenuIds { get; init; }
}
