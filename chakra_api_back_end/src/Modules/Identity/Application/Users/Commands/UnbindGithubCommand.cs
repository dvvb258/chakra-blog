using Identity.Application.Users.DTOs;
using SharedKernel.Application.Attributes;
using SharedKernel.Application.CQRS;
using SharedKernel.Constants;

namespace Identity.Application.Users.Commands;

/// <summary>
/// 解绑 GitHub 账号命令
/// </summary>
/// <param name="UserId">当前登录用户ID</param>
[AuditLog(AuditModules.Profile, "解绑 GitHub")]
public sealed record UnbindGithubCommand(long UserId) : ICommand<CurrentUserDto>;
