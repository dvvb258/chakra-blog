namespace Identity.Application.Users.DTOs;

/// <summary>
/// 更新系统菜单请求参数
/// </summary>
public sealed record UpdateMenuRequest
{
    /// <summary>
    /// 父级菜单 ID（为空表示根菜单节点）
    /// </summary>
    public long? ParentId { get; init; }

    /// <summary>
    /// 菜单名称（如：菜单管理）
    /// </summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>
    /// 前端路由路径（如：/menus）
    /// </summary>
    public string Path { get; init; } = string.Empty;

    /// <summary>
    /// 权限代号（如：menu:manage）
    /// </summary>
    public string? PermissionCode { get; init; }

    /// <summary>
    /// 菜单图标组件标识（如：AppstoreOutlined）
    /// </summary>
    public string? Icon { get; init; }

    /// <summary>
    /// 显示排序权重（升序）
    /// </summary>
    public int Sort { get; init; }

    /// <summary>
    /// 是否在侧边栏中隐藏
    /// </summary>
    public bool IsHidden { get; init; }

    /// <summary>
    /// 资源类型（1: 目录, 2: 菜单/页面, 3: 按钮/操作权限，默认 2）
    /// </summary>
    public int? Type { get; init; }
}
