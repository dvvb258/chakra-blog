namespace Identity.Application.Users.DTOs;

/// <summary>
/// GitHub OAuth 登录请求参数
/// </summary>
public sealed record GitHubLoginRequest
{
    /// <summary>
    /// GitHub OAuth 回调授权码
    /// </summary>
    public string Code { get; init; } = string.Empty;

    /// <summary>
    /// GitHub OAuth 回调状态值
    /// </summary>
    public string State { get; init; } = string.Empty;
}
