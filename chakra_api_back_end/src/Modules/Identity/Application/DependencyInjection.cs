using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace Identity.Application;

/// <summary>
/// Identity 模块 Application 层依赖注入扩展
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// 注册 Identity Application 层服务（FluentValidation 验证器）
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <returns>服务集合</returns>
    public static IServiceCollection AddIdentityApplication(this IServiceCollection services)
    {
        var assembly = typeof(DependencyInjection).Assembly;

        // 注册 FluentValidation 验证器（MediatR 统一在 ApiHost 注册）
        services.AddValidatorsFromAssembly(assembly);

        return services;
    }
}
