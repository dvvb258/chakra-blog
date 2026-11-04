using SharedKernel.Application.Attributes;
using SharedKernel.Application.CQRS;
using SharedKernel.Constants;

namespace Identity.Application.Users.Commands;

/// <summary>
/// 删除系统角色命令
/// </summary>
/// <param name="Id">角色ID</param>
[AuditLog(AuditModules.Role, "删除系统角色")]
public sealed record DeleteRoleCommand(long Id) : ICommand;
