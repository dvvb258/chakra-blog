namespace Logging.Application.Logs.DTOs;

/// <summary>
/// 系统异常日志 DTO
/// </summary>
public record ExceptionLogDto
{
    /// <summary>日志ID</summary>
    public long Id { get; init; }

    /// <summary>异常类型名称</summary>
    public string ExceptionType { get; init; } = string.Empty;

    /// <summary>异常错误摘要信息</summary>
    public string Message { get; init; } = string.Empty;

    /// <summary>完整异常堆栈追踪（StackTrace）</summary>
    public string? StackTrace { get; init; }

    /// <summary>请求路径</summary>
    public string Path { get; init; } = string.Empty;

    /// <summary>HTTP 请求方式</summary>
    public string Method { get; init; } = string.Empty;

    /// <summary>URL 查询参数</summary>
    public string? QueryString { get; init; }

    /// <summary>请求体快照</summary>
    public string? RequestBody { get; init; }

    /// <summary>客户端 IP</summary>
    public string? ClientIp { get; init; }

    /// <summary>用户ID</summary>
    public long? UserId { get; init; }

    /// <summary>用户名</summary>
    public string? Username { get; init; }

    /// <summary>全链路追踪 Trace ID</summary>
    public string? TraceId { get; init; }

    /// <summary>发生时间</summary>
    public DateTime CreatedAt { get; init; }
}
