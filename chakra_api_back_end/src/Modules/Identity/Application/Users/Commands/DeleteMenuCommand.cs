using SharedKernel.Application.Attributes;
using SharedKernel.Application.CQRS;
using SharedKernel.Constants;

namespace Identity.Application.Users.Commands;

/// <summary>
/// 删除系统菜单命令
/// </summary>
/// <param name="Id">菜单ID</param>
[AuditLog(AuditModules.Role, "删除系统菜单")]
public sealed record DeleteMenuCommand(long Id) : ICommand;
