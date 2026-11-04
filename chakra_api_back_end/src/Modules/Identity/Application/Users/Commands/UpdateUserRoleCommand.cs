using SharedKernel.Application.Attributes;
using SharedKernel.Application.CQRS;
using SharedKernel.Constants;

namespace Identity.Application.Users.Commands;

/// <summary>
/// 更新用户角色命令（仅限超级管理员）
/// </summary>
/// <param name="UserId">用户ID</param>
/// <param name="Role">目标角色（Admin 或 Editor）</param>
[AuditLog(AuditModules.User, "修改用户角色")]
public sealed record UpdateUserRoleCommand(long UserId, string Role) : ICommand;
