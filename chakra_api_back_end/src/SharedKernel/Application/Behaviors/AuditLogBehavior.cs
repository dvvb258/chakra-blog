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
        // 显式声明跳过审计的请求（如前台高频匿名统计打点）直接放行，避免海量请求冲垮管理端审计日志
        if (typeof(TRequest).GetCustomAttribute<SkipAuditLogAttribute>() != null)
        {
            return await next();
        }

        // 仅拦截 Command（写操作）或显式标记了 [AuditLog] 的请求
        var auditAttr = typeof(TRequest).GetCustomAttribute<AuditLogAttribute>();
        var isCommand = typeof(ICommand).IsAssignableFrom(typeof(TRequest)) ||
                        typeof(TRequest).GetInterfaces().Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(ICommand<>));

        if (!isCommand && auditAttr == null)
        {
            return await next();
        }

        // 模块与行为一律来自显式声明，未声明时回落到通用模块，避免依赖命名空间字符串猜测
        var module = auditAttr?.Module ?? AuditModules.System;
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

            // 构造审计日志并无阻塞压入内存通道（写入为内存操作，实际耗时远低于 DurationMs 记录的毫秒精度）
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
}
