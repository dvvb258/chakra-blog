namespace Identity.Application.Users.DTOs;

/// <summary>
/// GitHub 授权目标地址数据传输对象
/// </summary>
public sealed record GitHubAuthorizeDto
{
    /// <summary>
    /// GitHub OAuth 授权页面 URL
    /// </summary>
    public string Url { get; init; } = string.Empty;

    /// <summary>
    /// CSRF 校验随机状态值
    /// </summary>
    public string State { get; init; } = string.Empty;
}
