using Identity.Application.Users.DTOs;
using SharedKernel.Application.Attributes;
using SharedKernel.Application.CQRS;
using SharedKernel.Constants;

namespace Identity.Application.Users.Commands;

/// <summary>
/// 更新个人资料命令
/// </summary>
/// <param name="UserId">当前登录用户ID</param>
/// <param name="Request">更新资料参数</param>
[AuditLog(AuditModules.Profile, "修改个人资料")]
public sealed record UpdateProfileCommand(
    long UserId,
    UpdateProfileRequest Request) : ICommand<CurrentUserDto>;
