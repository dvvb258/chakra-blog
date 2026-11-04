namespace Logging.Application.Logs.DTOs;

/// <summary>
/// 业务操作审计日志 DTO
/// </summary>
public record AuditLogDto
{
    /// <summary>日志ID</summary>
    public long Id { get; init; }

    /// <summary>操作人用户ID</summary>
    public long? UserId { get; init; }

    /// <summary>操作人用户名</summary>
    public string? Username { get; init; }

    /// <summary>所属业务模块</summary>
    public string Module { get; init; } = string.Empty;

    /// <summary>具体操作行为</summary>
    public string Action { get; init; } = string.Empty;

    /// <summary>HTTP 请求方式</summary>
    public string Method { get; init; } = string.Empty;

    /// <summary>请求路径</summary>
    public string Path { get; init; } = string.Empty;

    /// <summary>参数摘要</summary>
    public string? Parameters { get; init; }

    /// <summary>客户端 IP</summary>
    public string? ClientIp { get; init; }

    /// <summary>执行耗时（毫秒）</summary>
    public long DurationMs { get; init; }

    /// <summary>是否执行成功</summary>
    public bool IsSuccess { get; init; }

    /// <summary>错误摘要</summary>
    public string? ErrorMessage { get; init; }

    /// <summary>全链路追踪 Trace ID</summary>
    public string? TraceId { get; init; }

    /// <summary>记录时间</summary>
    public DateTime CreatedAt { get; init; }
}
