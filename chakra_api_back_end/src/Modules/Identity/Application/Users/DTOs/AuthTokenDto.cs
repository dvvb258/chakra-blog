namespace Identity.Application.Users.DTOs;

/// <summary>
/// 登录/刷新令牌认证响应数据
/// </summary>
public sealed record AuthTokenDto
{
    /// <summary>
    /// JWT 访问令牌
    /// </summary>
    public string AccessToken { get; init; } = string.Empty;

    /// <summary>
    /// 刷新令牌（用于无感续期）
    /// </summary>
    public string RefreshToken { get; init; } = string.Empty;

    /// <summary>
    /// 令牌类型（固定 Bearer）
    /// </summary>
    public string TokenType { get; init; } = "Bearer";

    /// <summary>
    /// 访问令牌有效秒数
    /// </summary>
    public int ExpiresIn { get; init; }

    /// <summary>
    /// 当前登录人基本信息
    /// </summary>
    public CurrentUserDto User { get; init; } = null!;
}
