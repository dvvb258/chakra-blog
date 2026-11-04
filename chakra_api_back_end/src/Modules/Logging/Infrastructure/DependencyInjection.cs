using Logging.Infrastructure.Data;
using Logging.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SharedKernel.Application;

namespace Logging.Infrastructure;

/// <summary>
/// Logging 模块基础设施依赖注入扩展
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// 添加 Logging 模块全部服务（DbContext + 异步通道队列 + 后台持久化 Worker）
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <param name="configuration">全局配置</param>
    /// <returns>服务集合</returns>
    public static IServiceCollection AddLoggingModule(this IServiceCollection services, IConfiguration configuration)
    {
        var logConn = configuration.GetConnectionString("LogConnection");
        var connectionString = !string.IsNullOrWhiteSpace(logConn)
            ? logConn
            : configuration.GetConnectionString("DefaultConnection");

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException("未配置 LogConnection 或 DefaultConnection 数据库连接字符串");
        }

        services.AddDbContext<LoggingDbContext>(options =>
        {
            options.UseNpgsql(connectionString)
                   .UseSnakeCaseNamingConvention();
        });

        // 注册内存通道队列（单例）与后台批量落盘服务
        services.AddSingleton<IAuditLogQueue, AuditLogQueue>();
        services.AddHostedService<AuditLogProcessor>();

        return services;
    }
}
