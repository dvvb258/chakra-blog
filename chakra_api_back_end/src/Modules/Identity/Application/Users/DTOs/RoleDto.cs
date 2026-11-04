namespace Identity.Application.Users.DTOs;

/// <summary>
/// 角色信息数据传输对象（含关联菜单权限列表）
/// </summary>
public record RoleDto
{
    /// <summary>角色唯一 ID</summary>
    public long Id { get; init; }

    /// <summary>角色代号</summary>
    public string Code { get; init; } = string.Empty;

    /// <summary>角色显示名称</summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>角色职能描述</summary>
    public string Description { get; init; } = string.Empty;

    /// <summary>是否为系统内置角色（系统角色不可被删除）</summary>
    public bool IsSystem { get; init; }

    /// <summary>当前角色关联的菜单ID集合</summary>
    public List<long> MenuIds { get; init; } = [];
}
