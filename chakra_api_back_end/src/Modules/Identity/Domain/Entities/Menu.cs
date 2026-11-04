using Identity.Domain.Enums;
using SharedKernel.Domain;

namespace Identity.Domain.Entities;

/// <summary>
/// 系统菜单与权限资源实体
/// </summary>
public class Menu : Entity, IAggregateRoot
{
    /// <summary>
    /// 菜单资源类型（1: 目录, 2: 菜单/页面, 3: 按钮/操作权限）
    /// </summary>
    public MenuType Type { get; private set; } = MenuType.Menu;

    /// <summary>
    /// 父级菜单 ID（为空表示根节点）
    /// </summary>
    public long? ParentId { get; private set; }

    /// <summary>
    /// 菜单名称（如：仪表盘、文章管理）
    /// </summary>
    public string Name { get; private set; } = string.Empty;

    /// <summary>
    /// 前端路由路径（如：/、/articles、/logs）
    /// </summary>
    public string Path { get; private set; } = string.Empty;

    /// <summary>
    /// 权限代号（如：dashboard:view, article:manage）
    /// </summary>
    public string PermissionCode { get; private set; } = string.Empty;

    /// <summary>
    /// 菜单图标组件标识（如：DashboardOutlined）
    /// </summary>
    public string? Icon { get; private set; }

    /// <summary>
    /// 显示排序权重（升序）
    /// </summary>
    public int Sort { get; private set; }

    /// <summary>
    /// 是否在侧边栏中隐藏
    /// </summary>
    public bool IsHidden { get; private set; }

    private Menu()
        : base()
    {
    }

    /// <summary>
    /// 初始化系统菜单实体
    /// </summary>
    /// <param name="name">菜单名称</param>
    /// <param name="path">路由路径</param>
    /// <param name="permissionCode">权限代号</param>
    /// <param name="parentId">父级ID</param>
    /// <param name="icon">图标标识</param>
    /// <param name="sort">排序权重</param>
    /// <param name="isHidden">是否隐藏</param>
    /// <param name="type">资源类型</param>
    /// <param name="id">指定实体ID（可选）</param>
    public Menu(
        string name,
        string path,
        string permissionCode,
        long? parentId = null,
        string? icon = null,
        int sort = 0,
        bool isHidden = false,
        MenuType type = MenuType.Menu,
        long? id = null)
        : base(id ?? SnowflakeIdGenerator.NextId())
    {
        Name = string.IsNullOrWhiteSpace(name) ? throw new ArgumentException("菜单名称不能为空", nameof(name)) : name.Trim();
        Path = string.IsNullOrWhiteSpace(path) ? "/" : path.Trim();
        PermissionCode = string.IsNullOrWhiteSpace(permissionCode) ? path : permissionCode.Trim();
        ParentId = parentId;
        Icon = icon?.Trim();
        Sort = sort;
        IsHidden = isHidden;
        Type = type;
    }

    /// <summary>
    /// 更新菜单信息
    /// </summary>
    /// <param name="name">菜单名称</param>
    /// <param name="path">路由路径</param>
    /// <param name="permissionCode">权限代号</param>
    /// <param name="parentId">父级ID</param>
    /// <param name="icon">图标组件名</param>
    /// <param name="sort">排序号</param>
    /// <param name="isHidden">是否隐藏</param>
    /// <param name="type">资源类型</param>
    public void Update(string name, string path, string permissionCode, long? parentId, string? icon, int sort, bool isHidden, MenuType type = MenuType.Menu)
    {
        Name = string.IsNullOrWhiteSpace(name) ? Name : name.Trim();
        Path = string.IsNullOrWhiteSpace(path) ? Path : path.Trim();
        PermissionCode = string.IsNullOrWhiteSpace(permissionCode) ? PermissionCode : permissionCode.Trim();
        ParentId = parentId;
        Icon = icon?.Trim();
        Sort = sort;
        IsHidden = isHidden;
        Type = type;
        SetUpdatedAt();
    }
}
