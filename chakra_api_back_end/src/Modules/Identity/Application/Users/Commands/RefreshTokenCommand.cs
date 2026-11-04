using Identity.Application.Users.DTOs;
using SharedKernel.Application.CQRS;

namespace Identity.Application.Users.Commands;

/// <summary>
/// 刷新令牌命令（通过有效的 RefreshToken 换取新的双 Token）
/// </summary>
/// <param name="RefreshToken">当前拥有的刷新令牌</param>
/// <param name="IpAddress">客户端IP</param>
public sealed record RefreshTokenCommand(
    string RefreshToken,
    string? IpAddress = null) : ICommand<AuthTokenDto>;
