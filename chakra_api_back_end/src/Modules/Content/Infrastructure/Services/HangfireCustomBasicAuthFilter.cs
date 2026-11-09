using System.Net.Http.Headers;
using System.Security.Cryptography;
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

                    // 使用常量时间比较，避免通过响应耗时逐字符探测口令
                    if (FixedTimeEquals(user, _username) & FixedTimeEquals(pass, _password))
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

    private static bool FixedTimeEquals(string? left, string? right)
    {
        var leftBytes = Encoding.UTF8.GetBytes(left ?? string.Empty);
        var rightBytes = Encoding.UTF8.GetBytes(right ?? string.Empty);

        return leftBytes.Length == rightBytes.Length
            && CryptographicOperations.FixedTimeEquals(leftBytes, rightBytes);
    }

    private static void SetChallengeResponse(HttpContext httpContext)
    {
        httpContext.Response.StatusCode = StatusCodes.Status401Unauthorized;
        httpContext.Response.Headers.Append("WWW-Authenticate", "Basic realm=\"Hangfire Dashboard\"");
    }
}
