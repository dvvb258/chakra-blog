namespace Identity.Application.Users.DTOs;

/// <summary>
/// 刷新令牌请求模型
/// </summary>
/// <param name="RefreshToken">当前有效的刷新令牌</param>
public sealed record RefreshTokenRequest(string RefreshToken);
