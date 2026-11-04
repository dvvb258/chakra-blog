namespace Identity.Application.Users.DTOs;

/// <summary>
/// 撤销令牌请求模型
/// </summary>
/// <param name="RefreshToken">要撤销作废的刷新令牌</param>
public sealed record RevokeTokenRequest(string RefreshToken);
