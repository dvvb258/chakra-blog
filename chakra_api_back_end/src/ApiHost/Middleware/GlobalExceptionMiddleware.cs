using Microsoft.Extensions.Options;
using SharedKernel.Application;
using SharedKernel.Application.Exceptions;
using System.Net;
using System.Text.Json;

namespace ApiHost.Middleware;

/// <summary>
/// 全局异常处理中间件
/// </summary>
public class GlobalExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionMiddleware> _logger;
    private readonly JsonSerializerOptions _jsonOptions;

    /// <summary>
    /// 初始化全局异常处理中间件
    /// </summary>
    /// <param name="next">下一个中间件</param>
    /// <param name="logger">日志记录器</param>
    /// <param name="jsonOptions">MVC JSON 序列化配置</param>
    public GlobalExceptionMiddleware(
        RequestDelegate next,
        ILogger<GlobalExceptionMiddleware> logger,
        IOptions<Microsoft.AspNetCore.Mvc.JsonOptions> jsonOptions)
    {
        _next = next;
        _logger = logger;
        _jsonOptions = jsonOptions.Value.JsonSerializerOptions;
    }

    /// <summary>
    /// 调用中间件
    /// </summary>
    /// <param name="context">HTTP上下文</param>
    /// <returns>任务</returns>
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var response = context.Response;

        if (response.HasStarted)
        {
            _logger.LogWarning("响应已开始向客户端发送，无法写入全局异常结果。");
            return;
        }

        response.ContentType = "application/json";

        var traceId = context.Items["TraceId"] as string ?? context.TraceIdentifier;

        ApiResult<object?> result;
        int statusCode;

        if (exception is BusinessException businessEx)
        {
            _logger.LogWarning("业务异常 [{Code}] [TraceId: {TraceId}]: {Message}", businessEx.Code, traceId, businessEx.Message);
            object? errors = (businessEx as ValidationException)?.Errors;

            result = new ApiResult<object?>
            {
                Code = businessEx.Code,
                Message = businessEx.Message,
                Data = errors,
                TraceId = traceId,
                Timestamp = DateTime.UtcNow
            };
            statusCode = businessEx.Code;
        }
        else
        {
            var detailedMessage = exception.InnerException != null
                ? $"{exception.Message} ---> {exception.InnerException.Message}"
                : exception.Message;

            _logger.LogError(exception, "未处理的异常 [TraceId: {TraceId}]: {Message}", traceId, detailedMessage);

            // 异步将系统未处理异常压入内存通道队列
            try
            {
                var auditLogQueue = context.RequestServices.GetService<IAuditLogQueue>();
                var currentUser = context.RequestServices.GetService<ICurrentUser>();

                auditLogQueue?.EnqueueExceptionLog(new ExceptionLogEntry
                {
                    ExceptionType = exception.GetType().FullName ?? exception.GetType().Name,
                    Message = detailedMessage,
                    StackTrace = exception.StackTrace,
                    Path = context.Request.Path.Value ?? string.Empty,
                    Method = context.Request.Method,
                    QueryString = context.Request.QueryString.Value,
                    ClientIp = context.Connection.RemoteIpAddress?.ToString(),
                    UserId = currentUser?.IsAuthenticated == true && currentUser.UserId > 0 ? currentUser.UserId : null,
                    Username = currentUser?.IsAuthenticated == true && !string.IsNullOrWhiteSpace(currentUser.Username) ? currentUser.Username : null,
                    TraceId = traceId,
                    CreatedAt = DateTime.UtcNow
                });
            }
            catch (Exception queueEx)
            {
                _logger.LogWarning(queueEx, "入队系统异常日志失败");
            }

            result = ApiResult<object?>.Error("服务器内部错误", (int)HttpStatusCode.InternalServerError, traceId);
            statusCode = (int)HttpStatusCode.InternalServerError;
        }

        response.StatusCode = statusCode;

        await response.WriteAsync(JsonSerializer.Serialize(result, _jsonOptions));
    }
}
