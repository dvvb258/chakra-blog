namespace SharedKernel.Application;

/// <summary>
/// 统一API返回格式
/// </summary>
/// <typeparam name="T">数据类型</typeparam>
public class ApiResult<T>
{
    /// <summary>
    /// 状态码
    /// </summary>
    public int Code { get; set; }

    /// <summary>
    /// 消息
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// 数据
    /// </summary>
    public T? Data { get; set; }

    /// <summary>
    /// 全链路请求追踪标识（Trace ID）
    /// </summary>
    public string? TraceId { get; set; }

    /// <summary>
    /// 时间戳
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// 返回成功结果
    /// </summary>
    /// <param name="data">返回的数据</param>
    /// <param name="message">提示消息</param>
    /// <param name="traceId">追踪ID</param>
    public static ApiResult<T> Success(T data, string message = "操作成功", string? traceId = null)
    {
        return new ApiResult<T>
        {
            Code = 200,
            Message = message,
            Data = data,
            TraceId = traceId
        };
    }

    /// <summary>
    /// 返回失败结果
    /// </summary>
    /// <param name="message">错误消息</param>
    /// <param name="code">状态码</param>
    /// <param name="traceId">追踪ID</param>
    public static ApiResult<T> Fail(string message, int code = 400, string? traceId = null)
    {
        return new ApiResult<T>
        {
            Code = code,
            Message = message,
            Data = default,
            TraceId = traceId
        };
    }

    /// <summary>
    /// 返回错误结果
    /// </summary>
    /// <param name="message">错误消息</param>
    /// <param name="code">状态码</param>
    /// <param name="traceId">追踪ID</param>
    public static ApiResult<T> Error(string message, int code = 500, string? traceId = null)
    {
        return new ApiResult<T>
        {
            Code = code,
            Message = message,
            Data = default,
            TraceId = traceId
        };
    }
}
