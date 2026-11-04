using Identity.Application.Users.DTOs;
using SharedKernel.Application.Attributes;
using SharedKernel.Application.CQRS;
using SharedKernel.Constants;

namespace Identity.Application.Users.Commands;

/// <summary>
/// 绑定 GitHub 账号命令
/// </summary>
/// <param name="UserId">当前登录用户ID</param>
/// <param name="Code">GitHub 临时授权码</param>
[AuditLog(AuditModules.Profile, "绑定 GitHub")]
public sealed record BindGithubCommand(
    long UserId,
    string Code) : ICommand<CurrentUserDto>;
