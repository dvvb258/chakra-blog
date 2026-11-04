using SharedKernel.Domain;

namespace Logging.Domain.Entities;

/// <summary>
/// 业务操作审计日志实体
/// </summary>
public class AuditLog : Entity
{
    /// <summary>操作人用户ID</summary>
    public long? UserId { get; private set; }

    /// <summary>操作人用户名/账号</summary>
    public string? Username { get; private set; }

    /// <summary>所属功能模块（如：文章管理、专栏管理、个人中心、身份认证）</summary>
    public string Module { get; private set; } = string.Empty;

    /// <summary>操作行为名称（如：发布文章、修改密码、删除专栏）</summary>
    public string Action { get; private set; } = string.Empty;

    /// <summary>HTTP 请求方式（POST, PUT, DELETE 等）</summary>
    public string Method { get; private set; } = string.Empty;

    /// <summary>请求路径</summary>
    public string Path { get; private set; } = string.Empty;

    /// <summary>请求参数摘要（已脱敏）</summary>
    public string? Parameters { get; private set; }

    /// <summary>客户端 IP 地址</summary>
    public string? ClientIp { get; private set; }

    /// <summary>执行耗时（毫秒）</summary>
    public long DurationMs { get; private set; }

    /// <summary>是否执行成功</summary>
    public bool IsSuccess { get; private set; }

    /// <summary>失败时的错误摘要</summary>
    public string? ErrorMessage { get; private set; }

    /// <summary>全链路追踪唯一标识 Trace ID</summary>
    public string? TraceId { get; private set; }

    private AuditLog()
        : base()
    {
    }

    public AuditLog(
        long? userId,
        string? username,
        string module,
        string action,
        string method,
        string path,
        string? parameters,
        string? clientIp,
        long durationMs,
        bool isSuccess,
        string? errorMessage = null,
        string? traceId = null)
        : base(SnowflakeIdGenerator.NextId())
    {
        UserId = userId;
        Username = username;
        Module = module;
        Action = action;
        Method = method;
        Path = path;
        Parameters = parameters;
        ClientIp = clientIp;
        DurationMs = durationMs;
        IsSuccess = isSuccess;
        ErrorMessage = errorMessage;
        TraceId = traceId;
    }
}
