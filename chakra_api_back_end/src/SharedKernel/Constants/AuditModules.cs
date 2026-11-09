namespace SharedKernel.Constants;

/// <summary>
/// 系统审计日志业务模块名称常量
/// </summary>
public static class AuditModules
{
    /// <summary>
    /// 文章管理模块
    /// </summary>
    public const string Article = "文章管理";

    /// <summary>
    /// 分类管理模块
    /// </summary>
    public const string Category = "分类管理";

    /// <summary>
    /// 专栏管理模块
    /// </summary>
    public const string Series = "专栏管理";

    /// <summary>
    /// 权限管理模块（角色与菜单）
    /// </summary>
    public const string Role = "权限管理";

    /// <summary>
    /// 用户管理模块
    /// </summary>
    public const string User = "用户管理";

    /// <summary>
    /// 个人中心模块
    /// </summary>
    public const string Profile = "个人中心";

    /// <summary>
    /// 身份认证模块（登录/Token续期等）
    /// </summary>
    public const string Auth = "身份认证";

    /// <summary>
    /// 站点设置模块
    /// </summary>
    public const string Settings = "站点设置";

    /// <summary>
    /// 未显式声明 [AuditLog] 时的兜底模块
    /// </summary>
    public const string System = "系统业务";
}
