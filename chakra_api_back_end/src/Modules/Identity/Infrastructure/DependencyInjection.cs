using Identity.Application;
using Identity.Application.Common;
using Identity.Application.Common.Interfaces;
using Identity.Domain.Entities;
using Identity.Infrastructure.Data;
using Identity.Infrastructure.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Identity.Infrastructure;

/// <summary>
/// Identity 模块依赖注入扩展
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// 添加 Identity 模块全部服务（Application + Infrastructure + DbContext + JWT + Hasher）
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <param name="configuration">全局配置</param>
    /// <returns>服务集合</returns>
    public static IServiceCollection AddIdentityModule(this IServiceCollection services, IConfiguration configuration)
    {
        // 1. Application 层（MediatR + FluentValidation）
        services.AddIdentityApplication();

        // 2. 绑定 JWT 与 GitHub 配置选项
        services.Configure<JwtSettings>(configuration.GetSection(JwtSettings.SectionName));
        services.Configure<GitHubSettings>(configuration.GetSection(GitHubSettings.SectionName));

        // 3. 注册数据库上下文（独立于 Content 模块，支持单库部署自动回退）
        var userConn = configuration.GetConnectionString("UserConnection");
        var connectionString = !string.IsNullOrWhiteSpace(userConn)
            ? userConn
            : configuration.GetConnectionString("DefaultConnection");

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException("未配置 UserConnection 或 DefaultConnection 数据库连接字符串");
        }

        services.AddDbContext<IdentityDbContext>(options =>
        {
            options.UseNpgsql(connectionString)
                   .UseSnakeCaseNamingConvention();
        });

        // 4. 注册 ASP.NET Core 原生密码哈希器
        services.AddScoped<IPasswordHasher<User>, PasswordHasher<User>>();

        // 5. 注册 JWT 令牌签发服务
        services.AddScoped<IJwtTokenService, JwtTokenService>();
        services.AddScoped<IUserAccessValidator, UserAccessValidator>();

        // 6. 注册动态 RBAC 权限验证器（依赖进程内缓存，避免每个请求多次查库）
        services.AddMemoryCache();
        services.AddScoped<SharedKernel.Application.Interfaces.IPermissionChecker, PermissionChecker>();

        // 7. 注册 GitHub OAuth 开放平台服务
        services.AddHttpClient<IGitHubOAuthService, GitHubOAuthService>();

        return services;
    }
}
