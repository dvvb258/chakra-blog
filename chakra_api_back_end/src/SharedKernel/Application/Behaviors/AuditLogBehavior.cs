using System.Diagnostics;
using System.Reflection;
using MediatR;
using Microsoft.AspNetCore.Http;
using SharedKernel.Application.Attributes;
using SharedKernel.Application.CQRS;
using SharedKernel.Constants;

namespace SharedKernel.Application.Behaviors;

/// <summary>
/// 业务操作审计日志管道行为（自动拦截写操作并异步非阻塞入队）
/// </summary>
public class AuditLogBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly IAuditLogQueue _auditLogQueue;
    private readonly ICurrentUser _currentUser;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public AuditLogBehavior(
        IAuditLogQueue auditLogQueue,
        ICurrentUser currentUser,
        IHttpContextAccessor httpContextAccessor)
    {
        _auditLogQueue = auditLogQueue;
        _currentUser = currentUser;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        // 仅拦截 Command（写操作）或显式标记了 [AuditLog] 的请求
        var auditAttr = typeof(TRequest).GetCustomAttribute<AuditLogAttribute>();
        var isCommand = typeof(ICommand).IsAssignableFrom(typeof(TRequest)) ||
                        typeof(TRequest).GetInterfaces().Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(ICommand<>));

        // 排除前台高频统计打点命令（如全站访问量、文章阅读量累加），防止海量匿名访客请求冲垮管理端审计日志
        var ns = typeof(TRequest).Namespace ?? string.Empty;
        var isStatsCommand = ns.Contains("Stats", StringComparison.OrdinalIgnoreCase);

        if ((!isCommand && auditAttr == null) || (isStatsCommand && auditAttr == null))
        {
            return await next();
        }

        var module = auditAttr?.Module ?? ResolveDefaultModule(typeof(TRequest));
        var action = auditAttr?.Action ?? typeof(TRequest).Name;

        var httpContext = _httpContextAccessor.HttpContext;
        var method = httpContext?.Request.Method ?? "INTERNAL";
        var path = httpContext?.Request.Path.Value ?? string.Empty;
        var clientIp = httpContext?.Connection.RemoteIpAddress?.ToString();
        var traceId = httpContext?.Items["TraceId"] as string ?? httpContext?.TraceIdentifier;

        var stopwatch = Stopwatch.StartNew();
        bool isSuccess = true;
        string? errorMessage = null;

        try
        {
            return await next();
        }
        catch (Exception ex)
        {
            isSuccess = false;
            errorMessage = ex.Message;
            throw;
        }
        finally
        {
            stopwatch.Stop();

            // 构造审计日志并微秒级无阻塞压入内存通道
            var entry = new AuditLogEntry
            {
                UserId = _currentUser.IsAuthenticated && _currentUser.UserId > 0 ? _currentUser.UserId : null,
                Username = _currentUser.IsAuthenticated && !string.IsNullOrWhiteSpace(_currentUser.Username) ? _currentUser.Username : null,
                Module = module,
                Action = action,
                Method = method,
                Path = path,
                ClientIp = clientIp,
                DurationMs = stopwatch.ElapsedMilliseconds,
                IsSuccess = isSuccess,
                ErrorMessage = errorMessage,
                TraceId = traceId,
                CreatedAt = DateTime.UtcNow
            };

            _auditLogQueue.EnqueueAuditLog(entry);
        }
    }

    private static string ResolveDefaultModule(Type requestType)
    {
        var ns = requestType.Namespace ?? string.Empty;
        if (ns.Contains("Articles")) return AuditModules.Article;
        if (ns.Contains("Series")) return AuditModules.Series;
        if (ns.Contains("Users")) return AuditModules.Profile;
        return "系统业务";
    }
}
