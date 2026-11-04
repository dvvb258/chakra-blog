namespace Identity.Domain.Enums;

/// <summary>
/// 菜单资源类型
/// </summary>
public enum MenuType
{
    /// <summary>
    /// 目录（侧边栏折叠项，无实际页面路由）
    /// </summary>
    Directory = 1,

    /// <summary>
    /// 菜单/页面（有页面路由，可在浏览器渲染页面）
    /// </summary>
    Menu = 2,

    /// <summary>
    /// 按钮/操作权限（无页面路由，对应页面内的具体按钮与后端接口操作）
    /// </summary>
    Button = 3,
}
