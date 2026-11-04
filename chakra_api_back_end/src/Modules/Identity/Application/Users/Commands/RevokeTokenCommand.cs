using SharedKernel.Application.CQRS;

namespace Identity.Application.Users.Commands;

/// <summary>
/// 撤销刷新令牌命令（主动登出或注销当前设备）
/// </summary>
/// <param name="RefreshToken">要撤销的刷新令牌</param>
public sealed record RevokeTokenCommand(string RefreshToken) : ICommand;
