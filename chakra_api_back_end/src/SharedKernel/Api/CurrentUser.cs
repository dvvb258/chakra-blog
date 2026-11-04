using System.Security.Claims;
using Microsoft.AspNetCore.Http;

namespace SharedKernel.Api;

/// <summary>
/// 从 HttpContext JWT Claims 读取当前用户
/// </summary>
public class CurrentUser : Application.ICurrentUser
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    /// <summary>
    /// 初始化当前用户实例
    /// </summary>
    /// <param name="httpContextAccessor">HttpContext 访问器</param>
    public CurrentUser(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    /// <inheritdoc />
    public long UserId
    {
        get
        {
            var value = _httpContextAccessor.HttpContext?.User
                .FindFirst(ClaimTypes.NameIdentifier)?.Value
                ?? _httpContextAccessor.HttpContext?.User
                .FindFirst("sub")?.Value
                ?? _httpContextAccessor.HttpContext?.User
                .FindFirst("nameid")?.Value
                ?? _httpContextAccessor.HttpContext?.User
                .FindFirst("id")?.Value;

            return long.TryParse(value, out var id) ? id : 0;
        }
    }

    /// <inheritdoc />
    public string Username =>
        _httpContextAccessor.HttpContext?.User
            .FindFirst(ClaimTypes.Name)?.Value
        ?? _httpContextAccessor.HttpContext?.User
            .FindFirst("unique_name")?.Value
        ?? _httpContextAccessor.HttpContext?.User
            .FindFirst("name")?.Value
        ?? string.Empty;

    /// <inheritdoc />
    public bool IsAuthenticated =>
        _httpContextAccessor.HttpContext?.User.Identity?.IsAuthenticated ?? false;

    /// <inheritdoc />
    public bool IsInRole(string role) =>
        _httpContextAccessor.HttpContext?.User.IsInRole(role) ?? false;
}
