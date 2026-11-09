namespace SharedKernel.Application.Attributes;

/// <summary>
/// 跳过审计日志特性：显式标记无需记录审计日志的 Command（如前台高频匿名打点命令）
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
public sealed class SkipAuditLogAttribute : Attribute
{
}
