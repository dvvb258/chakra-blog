namespace SharedKernel.Constants;

/// <summary>
/// 系统权限资源标识代号常量（对应 menus 表中 permission_code 字段）
/// <para>系统采用两层权限模型：</para>
/// <para>1. 一级菜单/路由容器标识（用于导航树结构组织，不参与叶子节点操作鉴权）；</para>
/// <para>2. 细粒度业务操作权限（用于 Controller 的 [RequirePermission] 特性及前端按钮守卫）。</para>
/// </summary>
public static class Permissions
{
    // =========================================================================
    // 一、系统一级菜单与路由容器标识（Menu Nodes）
    // 对应 menus 表中的顶级目录节点，用于动态菜单树组织与路由守卫
    // =========================================================================

    /// <summary>文章管理菜单容器</summary>
    public const string ArticleManage = "article:manage";

    /// <summary>技术分类菜单容器</summary>
    public const string CategoryManage = "category:manage";

    /// <summary>专栏系列菜单容器</summary>
    public const string SeriesManage = "series:manage";

    /// <summary>系统日志菜单容器</summary>
    public const string LogView = "log:view";

    /// <summary>角色权限菜单容器</summary>
    public const string RoleManage = "role:manage";

    /// <summary>菜单管理菜单容器</summary>
    public const string MenuManage = "menu:manage";

    /// <summary>用户与个人中心菜单容器</summary>
    public const string UserManage = "user:manage";

    // =========================================================================
    // 二、细粒度业务操作权限（Action Permissions）
    // 用于 Controller 接口 [RequirePermission] 鉴权与前端叶子权限控制
    // =========================================================================

    /// <summary>查看仪表盘数据</summary>
    public const string DashboardView = "dashboard:view";

    /// <summary>查看文章列表与详情</summary>
    public const string ArticleView = "content:article:view";

    /// <summary>创建新文章</summary>
    public const string ArticleCreate = "content:article:create";

    /// <summary>编辑与保存文章</summary>
    public const string ArticleEdit = "content:article:edit";

    /// <summary>删除文章</summary>
    public const string ArticleDelete = "content:article:delete";

    /// <summary>审核与发布文章</summary>
    public const string ArticleApprove = "content:article:approve";

    /// <summary>提交文章审核</summary>
    public const string ArticleSubmit = "content:article:submit";

    /// <summary>查看技术分类</summary>
    public const string CategoryView = "content:category:view";

    /// <summary>创建技术分类</summary>
    public const string CategoryCreate = "content:category:create";

    /// <summary>编辑技术分类</summary>
    public const string CategoryEdit = "content:category:edit";

    /// <summary>删除技术分类</summary>
    public const string CategoryDelete = "content:category:delete";

    /// <summary>查看专栏系列</summary>
    public const string SeriesView = "content:series:view";

    /// <summary>创建专栏系列</summary>
    public const string SeriesCreate = "content:series:create";

    /// <summary>编辑专栏系列</summary>
    public const string SeriesEdit = "content:series:edit";

    /// <summary>删除专栏系列</summary>
    public const string SeriesDelete = "content:series:delete";

    /// <summary>查看业务操作审计日志</summary>
    public const string LogAuditView = "system:log:audit:view";

    /// <summary>查看系统未处理异常日志</summary>
    public const string LogExceptionView = "system:log:exception:view";

    /// <summary>查看系统角色列表</summary>
    public const string RoleView = "system:role:view";

    /// <summary>创建系统角色</summary>
    public const string RoleCreate = "system:role:create";

    /// <summary>编辑角色信息与权限</summary>
    public const string RoleEdit = "system:role:edit";

    /// <summary>删除系统角色</summary>
    public const string RoleDelete = "system:role:delete";

    /// <summary>查看系统菜单树</summary>
    public const string MenuView = "system:menu:view";

    /// <summary>维护菜单与权限项（创建/编辑/删除）</summary>
    public const string MenuEdit = "system:menu:edit";

    /// <summary>查看用户列表</summary>
    public const string UserView = "system:user:view";

    /// <summary>分配用户角色</summary>
    public const string UserRoleUpdate = "system:user:role:update";

    /// <summary>维护站点设置（站名、SEO 信息与展示文案）</summary>
    public const string SettingsManage = "system:settings:manage";

    /// <summary>
    /// 系统所有标准权限元数据集合（按模块组织，供角色授权管理界面勾选）
    /// </summary>
    public static readonly IReadOnlyList<PermissionMetadata> All =
    [

        // 仪表盘
        new(DashboardView, "查看仪表盘", "仪表盘", "查看全站运营大屏统计与实时监控数据"),

        // 文章管理
        new(ArticleView, "查看文章", "文章管理", "查看文章列表及完整 Markdown 内容详情"),
        new(ArticleCreate, "新建文章", "文章管理", "创建与发布新技术文章"),
        new(ArticleEdit, "编辑文章", "文章管理", "修改文章正文、分类、封面及元数据"),
        new(ArticleDelete, "删除文章", "文章管理", "软删除技术文章"),
        new(ArticleApprove, "审核文章", "文章管理", "审核文章发布申请与退回驳回操作"),
        new(ArticleSubmit, "提交审核", "文章管理", "提交个人草稿至管理员审核发布"),

        // 分类管理
        new(CategoryView, "查看分类", "分类管理", "查看技术分类列表及已收录篇数"),
        new(CategoryCreate, "新建分类", "分类管理", "创建全新的文章技术分类"),
        new(CategoryEdit, "编辑分类", "分类管理", "修改分类名称、图标及排序号"),
        new(CategoryDelete, "删除分类", "分类管理", "删除分类（受下属文章保护拦截）"),

        // 专栏系列
        new(SeriesView, "查看专栏", "专栏管理", "查看专题专栏系列列表及收录文章"),
        new(SeriesCreate, "新建专栏", "专栏管理", "创建新专题专栏系列并配置主题色彩"),
        new(SeriesEdit, "编辑专栏", "专栏管理", "修改专栏系列信息及封面"),
        new(SeriesDelete, "删除专栏", "专栏管理", "删除专栏系列并自动解绑收录文章"),

        // 系统日志
        new(LogAuditView, "审计日志", "系统日志", "分页多维筛选查询业务操作审计日志"),
        new(LogExceptionView, "异常日志", "系统日志", "查询系统未处理异常日志及完整堆栈追踪"),

        // 角色权限
        new(RoleView, "查看角色", "角色权限", "查看系统角色列表与已分配菜单权限"),
        new(RoleCreate, "新建角色", "角色权限", "创建自定义系统角色"),
        new(RoleEdit, "编辑角色", "角色权限", "配置角色名称、描述及树形权限勾选"),
        new(RoleDelete, "删除角色", "角色权限", "删除自定义角色（受系统角色防死锁保护）"),

        // 菜单管理
        new(MenuView, "查看菜单", "菜单管理", "查看全量系统菜单与权限树列表"),
        new(MenuEdit, "维护菜单", "菜单管理", "创建、编辑与删除系统菜单和按钮权限项"),

        // 用户管理
        new(UserView, "查看用户", "用户管理", "查看系统用户列表与最后登录时间"),
        new(UserRoleUpdate, "分配角色", "用户管理", "为用户调整分配系统角色"),

        // 站点设置
        new(SettingsManage, "站点设置", "站点设置", "维护站名、SEO 信息、作者资料与展示文案"),
    ];
}
