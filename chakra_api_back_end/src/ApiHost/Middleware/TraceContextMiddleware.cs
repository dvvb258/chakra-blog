using System.Diagnostics;

namespace ApiHost.Middleware;

/// <summary>
/// 全链路请求追踪与 API 慢请求监控中间件
/// </summary>
public sealed class TraceContextMiddleware
{
    private const string HeaderTraceId = "X-Trace-Id";
    private const int SlowRequestThresholdMs = 500; // 慢请求阈值（毫秒）

    private readonly RequestDelegate _next;
    private readonly ILogger<TraceContextMiddleware> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="TraceContextMiddleware"/> class.
    /// </summary>
    /// <param name="next">中间件调用委托</param>
    /// <param name="logger">日志记录器</param>
    public TraceContextMiddleware(RequestDelegate next, ILogger<TraceContextMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    /// <summary>
    /// 执行中间件逻辑
    /// </summary>
    /// <param name="context">HTTP 上下文</param>
    /// <returns>异步任务</returns>
    public async Task InvokeAsync(HttpContext context)
    {
        // 1. 提取或生成唯一 Trace ID
        var traceId = context.Request.Headers[HeaderTraceId].FirstOrDefault();
        if (string.IsNullOrWhiteSpace(traceId))
        {
            traceId = Activity.Current?.Id ?? context.TraceIdentifier;
            if (string.IsNullOrWhiteSpace(traceId))
            {
                traceId = Guid.NewGuid().ToString("N");
            }
        }

        // 规范化 traceId，并存入上下文供业务层、日志行为和全局异常中间件提取
        context.Items["TraceId"] = traceId;

        // 2. 在响应头附加 X-Trace-Id
        context.Response.OnStarting(() =>
        {
            context.Response.Headers[HeaderTraceId] = traceId;
            return Task.CompletedTask;
        });

        // 3. 性能耗时监测
        var stopwatch = Stopwatch.StartNew();

        try
        {
            await _next(context);
        }
        finally
        {
            stopwatch.Stop();
            var elapsedMs = stopwatch.ElapsedMilliseconds;
            var statusCode = context.Response.StatusCode;
            var method = context.Request.Method;
            var path = context.Request.Path.Value ?? string.Empty;

            // 针对慢请求（>= 500ms）进行结构化告警记录（涵盖所有 GET 查询与写操作）
            if (elapsedMs >= SlowRequestThresholdMs)
            {
                _logger.LogWarning(
                    "【API 慢请求告警】[TraceId: {TraceId}] {Method} {Path} -> {StatusCode} 耗时: {ElapsedMs}ms（超过 {Threshold}ms 阈值）",
                    traceId,
                    method,
                    path,
                    statusCode,
                    elapsedMs,
                    SlowRequestThresholdMs);
            }
            else if (statusCode >= 500)
            {
                _logger.LogError(
                    "【API 服务端异常】[TraceId: {TraceId}] {Method} {Path} -> {StatusCode} 耗时: {ElapsedMs}ms",
                    traceId,
                    method,
                    path,
                    statusCode,
                    elapsedMs);
            }
        }
    }
}
