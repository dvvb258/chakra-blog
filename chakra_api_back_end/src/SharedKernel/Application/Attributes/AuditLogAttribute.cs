namespace SharedKernel.Application.Attributes;

/// <summary>
/// 业务操作审计日志特性（用于标记 Command 的模块与行为描述）
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
public sealed class AuditLogAttribute : Attribute
{
    /// <summary>
    /// 所属业务模块（如：文章管理、专栏管理、个人中心、身份认证）
    /// </summary>
    public string Module { get; }

    /// <summary>
    /// 具体操作行为描述（如：发布文章、修改密码、删除专栏）
    /// </summary>
    public string Action { get; }

    /// <summary>
    /// 初始化审计日志特性
    /// </summary>
    /// <param name="module">所属业务模块</param>
    /// <param name="action">具体操作行为描述</param>
    public AuditLogAttribute(string module, string action)
    {
        Module = module;
        Action = action;
    }
}
