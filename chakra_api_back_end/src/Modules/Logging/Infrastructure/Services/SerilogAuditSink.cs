using Serilog.Core;
using Serilog.Events;
using SharedKernel.Application;

namespace Logging.Infrastructure.Services;

/// <summary>
/// Serilog 自定义接收端：自动拦截 Warning / Error / Fatal 结构化日志并异步推入 IAuditLogQueue
/// </summary>
public sealed class SerilogAuditSink : ILogEventSink
{
    private readonly IAuditLogQueue _queue;

    public SerilogAuditSink(IAuditLogQueue queue)
    {
        _queue = queue;
    }

    public void Emit(LogEvent logEvent)
    {
        // 仅保存 Error/Fatal，或确实携带异常对象的 Warning
        if (logEvent.Level < LogEventLevel.Error && logEvent.Exception == null)
        {
            return;
        }

        // 过滤微软框架本身的常规心跳/路由噪音（保留应用自身业务日志和所有 Error）
        if (logEvent.Properties.TryGetValue("SourceContext", out var sourceContextVal))
        {
            var sourceContext = sourceContextVal.ToString().Trim('"');
            if (sourceContext.StartsWith("Microsoft.AspNetCore.Hosting") && logEvent.Level < LogEventLevel.Error)
            {
                return;
            }

            if (sourceContext.StartsWith("MediatR.Licensing", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            if (sourceContext.StartsWith("Logging.Infrastructure.Services.AuditLogProcessor", StringComparison.Ordinal) ||
                sourceContext.StartsWith("ApiHost.Middleware.GlobalExceptionMiddleware", StringComparison.Ordinal) ||
                sourceContext.StartsWith("ApiHost.Middleware.TraceContextMiddleware", StringComparison.Ordinal))
            {
                return;
            }
        }

        var message = logEvent.RenderMessage();

        // 过滤 Lucky Penny / MediatR 商业许可提示日志
        if (message.Contains("Lucky Penny", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        var ex = logEvent.Exception;

        var exceptionType = ex != null ? (ex.GetType().FullName ?? ex.GetType().Name) : $"[{logEvent.Level}]";
        var stackTrace = ex?.StackTrace;

        string path = string.Empty;
        string method = string.Empty;

        if (logEvent.Properties.TryGetValue("RequestPath", out var pathVal))
        {
            path = pathVal.ToString().Trim('"');
        }

        if (logEvent.Properties.TryGetValue("RequestMethod", out var methodVal))
        {
            method = methodVal.ToString().Trim('"');
        }

        var entry = new ExceptionLogEntry
        {
            ExceptionType = exceptionType,
            Message = message,
            StackTrace = stackTrace,
            Path = path,
            Method = method,
            CreatedAt = logEvent.Timestamp.UtcDateTime
        };

        _queue.EnqueueExceptionLog(entry);
    }
}
