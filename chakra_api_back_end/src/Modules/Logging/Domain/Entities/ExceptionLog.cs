using SharedKernel.Domain;

namespace Logging.Domain.Entities;

/// <summary>
/// 系统未处理异常日志实体
/// </summary>
public class ExceptionLog : Entity
{
    /// <summary>异常类型名称（如 System.NullReferenceException）</summary>
    public string ExceptionType { get; private set; } = string.Empty;

    /// <summary>异常错误摘要信息</summary>
    public string Message { get; private set; } = string.Empty;

    /// <summary>异常完整堆栈追踪信息（StackTrace）</summary>
    public string? StackTrace { get; private set; }

    /// <summary>请求路径</summary>
    public string Path { get; private set; } = string.Empty;

    /// <summary>HTTP 请求方式（GET, POST 等）</summary>
    public string Method { get; private set; } = string.Empty;

    /// <summary>URL 查询字符串</summary>
    public string? QueryString { get; private set; }

    /// <summary>请求体内容快照（可选）</summary>
    public string? RequestBody { get; private set; }

    /// <summary>客户端 IP 地址</summary>
    public string? ClientIp { get; private set; }

    /// <summary>发生异常时的登录用户 ID</summary>
    public long? UserId { get; private set; }

    /// <summary>发生异常时的登录用户名</summary>
    public string? Username { get; private set; }

    /// <summary>全链路追踪唯一标识 Trace ID</summary>
    public string? TraceId { get; private set; }

    private ExceptionLog()
        : base()
    {
    }

    public ExceptionLog(
        string exceptionType,
        string message,
        string? stackTrace,
        string path,
        string method,
        string? queryString,
        string? requestBody,
        string? clientIp,
        long? userId,
        string? username,
        string? traceId = null)
        : base(SnowflakeIdGenerator.NextId())
    {
        ExceptionType = exceptionType;
        Message = message;
        StackTrace = stackTrace;
        Path = path;
        Method = method;
        QueryString = queryString;
        RequestBody = requestBody;
        ClientIp = clientIp;
        UserId = userId;
        Username = username;
        TraceId = traceId;
    }
}
