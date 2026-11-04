namespace Identity.Application.Users.DTOs;

/// <summary>
/// 绑定 GitHub 账号请求体
/// </summary>
public sealed record BindGithubRequest
{
    /// <summary>
    /// GitHub 临时授权码（通过 OAuth 回调获取）
    /// </summary>
    public string Code { get; init; } = string.Empty;

    public string State { get; init; } = string.Empty;
}
