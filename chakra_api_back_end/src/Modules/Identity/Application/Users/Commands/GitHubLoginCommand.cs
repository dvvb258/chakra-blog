using Identity.Application.Users.DTOs;
using SharedKernel.Application.Attributes;
using SharedKernel.Application.CQRS;
using SharedKernel.Constants;

namespace Identity.Application.Users.Commands;

/// <summary>
/// GitHub OAuth 登录凭据置换命令
/// </summary>
/// <param name="Code">GitHub 授权临时授权码</param>
/// <param name="IpAddress">客户端请求 IP 地址</param>
[AuditLog(AuditModules.Auth, "GitHub 快捷登录")]
public sealed record GitHubLoginCommand(
    string Code,
    string? IpAddress = null) : ICommand<AuthTokenDto>;
