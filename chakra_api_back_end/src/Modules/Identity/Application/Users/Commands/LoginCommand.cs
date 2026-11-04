using Identity.Application.Users.DTOs;
using SharedKernel.Application.Attributes;
using SharedKernel.Application.CQRS;
using SharedKernel.Constants;

namespace Identity.Application.Users.Commands;

/// <summary>
/// 管理员账号密码登录命令
/// </summary>
/// <param name="Account">登录凭证（支持用户名或邮箱）</param>
/// <param name="Password">明文密码</param>
/// <param name="IpAddress">发起登录的客户端IP</param>
[AuditLog(AuditModules.Auth, "账号密码登录")]
public sealed record LoginCommand(
    string Account,
    string Password,
    string? IpAddress = null) : ICommand<AuthTokenDto>;
