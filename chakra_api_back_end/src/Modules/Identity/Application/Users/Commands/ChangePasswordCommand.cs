using Identity.Application.Users.DTOs;
using SharedKernel.Application.Attributes;
using SharedKernel.Application.CQRS;
using SharedKernel.Constants;

namespace Identity.Application.Users.Commands;

/// <summary>
/// 修改/设置密码命令
/// </summary>
/// <param name="UserId">当前登录用户ID</param>
/// <param name="Request">修改密码请求参数</param>
[AuditLog(AuditModules.Profile, "修改安全密码")]
public sealed record ChangePasswordCommand(
    long UserId,
    ChangePasswordRequest Request) : ICommand<string>;
