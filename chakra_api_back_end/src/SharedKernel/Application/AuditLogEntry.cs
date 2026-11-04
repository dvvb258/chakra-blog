namespace SharedKernel.Application;

/// <summary>
/// 业务操作审计日志条目
/// </summary>
public record AuditLogEntry
{
    /// <summary>操作人用户ID</summary>
    public long? UserId { get; init; }

    /// <summary>操作人账号/用户名</summary>
    public string? Username { get; init; }

    /// <summary>所属功能模块（如：文章管理、专栏管理、个人中心、身份认证）</summary>
    public string Module { get; init; } = string.Empty;

    /// <summary>操作行为描述（如：发布文章、修改密码、删除专栏）</summary>
    public string Action { get; init; } = string.Empty;

    /// <summary>HTTP 请求方式（POST, PUT, DELETE）</summary>
    public string Method { get; init; } = string.Empty;

    /// <summary>请求路径</summary>
    public string Path { get; init; } = string.Empty;

    /// <summary>请求参数摘要（已脱敏）</summary>
    public string? Parameters { get; init; }

    /// <summary>客户端 IP</summary>
    public string? ClientIp { get; init; }

    /// <summary>执行耗时（毫秒）</summary>
    public long DurationMs { get; init; }

    /// <summary>是否执行成功</summary>
    public bool IsSuccess { get; init; }

    /// <summary>错误消息摘要</summary>
    public string? ErrorMessage { get; init; }

    /// <summary>全链路追踪 Trace ID</summary>
    public string? TraceId { get; init; }

    /// <summary>发生时间（UTC）</summary>
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
}
