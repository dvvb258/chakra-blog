using System.Net.Http.Headers;
using System.Text;
using Hangfire.Dashboard;
using Microsoft.AspNetCore.Http;

namespace Content.Infrastructure.Services;

/// <summary>
/// Hangfire 控制面板 HTTP Basic 认证过滤器（防止生产环境裸奔）
/// </summary>
public sealed class HangfireCustomBasicAuthFilter : IDashboardAuthorizationFilter
{
    private readonly string _username;
    private readonly string _password;

    /// <summary>
    /// 初始化 Hangfire 控制面板 Basic 认证过滤器
    /// </summary>
    /// <param name="username">允许登录的用户名</param>
    /// <param name="password">允许登录的密码</param>
    public HangfireCustomBasicAuthFilter(string username, string password)
    {
        _username = username;
        _password = password;
    }

    /// <inheritdoc />
    public bool Authorize(DashboardContext context)
    {
        var httpContext = context.GetHttpContext();

        var authHeader = httpContext.Request.Headers.Authorization.ToString();
        if (AuthenticationHeaderValue.TryParse(authHeader, out var headerValue)
            && string.Equals(headerValue.Scheme, "Basic", StringComparison.OrdinalIgnoreCase)
            && !string.IsNullOrWhiteSpace(headerValue.Parameter))
        {
            try
            {
                var encoding = Encoding.UTF8;
                var credentials = encoding.GetString(Convert.FromBase64String(headerValue.Parameter)).Split(':', 2);
                if (credentials.Length == 2)
                {
                    var user = credentials[0];
                    var pass = credentials[1];

                    if (string.Equals(user, _username, StringComparison.Ordinal)
                        && string.Equals(pass, _password, StringComparison.Ordinal))
                    {
                        return true;
                    }
                }
            }
            catch
            {
                // 忽略 Base64 格式解析异常，返回 401 挑战
            }
        }

        SetChallengeResponse(httpContext);
        return false;
    }

    private static void SetChallengeResponse(HttpContext httpContext)
    {
        httpContext.Response.StatusCode = StatusCodes.Status401Unauthorized;
        httpContext.Response.Headers.Append("WWW-Authenticate", "Basic realm=\"Hangfire Dashboard\"");
    }
}
