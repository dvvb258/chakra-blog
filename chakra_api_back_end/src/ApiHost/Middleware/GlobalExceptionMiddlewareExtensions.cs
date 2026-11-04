namespace ApiHost.Middleware;

/// <summary>
/// 全局异常中间件扩展
/// </summary>
public static class GlobalExceptionMiddlewareExtensions
{
    /// <summary>
    /// 使用全局异常处理中间件
    /// </summary>
    /// <param name="app">应用构建器</param>
    /// <returns>应用构建器</returns>
    public static IApplicationBuilder UseGlobalExceptionHandler(this IApplicationBuilder app)
    {
        return app.UseMiddleware<GlobalExceptionMiddleware>();
    }
}
