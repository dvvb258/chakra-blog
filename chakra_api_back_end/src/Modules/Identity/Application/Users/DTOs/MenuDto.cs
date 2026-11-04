namespace Identity.Application.Users.DTOs;

/// <summary>
/// 菜单与权限信息数据传输对象
/// </summary>
public sealed record MenuDto
{
    /// <summary>
    /// 菜单唯一 ID
    /// </summary>
    public long Id { get; init; }

    /// <summary>
    /// 父级菜单 ID
    /// </summary>
    public long? ParentId { get; init; }

    /// <summary>
    /// 菜单名称
    /// </summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>
    /// 前端路由路径
    /// </summary>
    public string Path { get; init; } = string.Empty;

    /// <summary>
    /// 权限代号
    /// </summary>
    public string PermissionCode { get; init; } = string.Empty;

    /// <summary>
    /// 图标组件名
    /// </summary>
    public string? Icon { get; init; }

    /// <summary>
    /// 排序权重
    /// </summary>
    public int Sort { get; init; }

    /// <summary>
    /// 是否在侧边栏隐藏
    /// </summary>
    public bool IsHidden { get; init; }

    /// <summary>
    /// 资源类型（1: 目录, 2: 菜单/页面, 3: 按钮/操作权限）
    /// </summary>
    public int Type { get; init; } = 2;

    /// <summary>
    /// 子菜单节点树（可选）
    /// </summary>
    public List<MenuDto> Children { get; init; } = [];
}
