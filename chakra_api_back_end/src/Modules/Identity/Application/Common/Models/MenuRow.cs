using Identity.Application.Users.DTOs;

namespace Identity.Application.Common.Models;

/// <summary>
/// 菜单与权限资源只读行（Dapper 读模型）
/// </summary>
public sealed record MenuRow
{
    /// <summary>菜单唯一 ID</summary>
    public long Id { get; init; }

    /// <summary>父级菜单 ID</summary>
    public long? ParentId { get; init; }

    /// <summary>菜单名称</summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>前端路由路径</summary>
    public string Path { get; init; } = string.Empty;

    /// <summary>权限代号</summary>
    public string PermissionCode { get; init; } = string.Empty;

    /// <summary>图标组件名</summary>
    public string? Icon { get; init; }

    /// <summary>排序权重</summary>
    public int Sort { get; init; }

    /// <summary>是否在侧边栏隐藏</summary>
    public bool IsHidden { get; init; }

    /// <summary>资源类型（1: 目录, 2: 菜单/页面, 3: 按钮/操作权限）</summary>
    public int Type { get; init; }

    /// <summary>
    /// 转换为菜单数据传输对象
    /// </summary>
    /// <returns>菜单 DTO</returns>
    public MenuDto ToDto()
    {
        return new MenuDto
        {
            Id = Id,
            ParentId = ParentId,
            Name = Name,
            Path = Path,
            PermissionCode = PermissionCode,
            Icon = Icon,
            Sort = Sort,
            IsHidden = IsHidden,
            Type = Type,
        };
    }
}
