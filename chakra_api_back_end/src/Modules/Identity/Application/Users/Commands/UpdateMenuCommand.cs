using Identity.Application.Users.DTOs;
using SharedKernel.Application.Attributes;
using SharedKernel.Application.CQRS;
using SharedKernel.Constants;

namespace Identity.Application.Users.Commands;

/// <summary>
/// 更新系统菜单命令
/// </summary>
/// <param name="Id">菜单ID</param>
/// <param name="Request">更新系统菜单参数</param>
[AuditLog(AuditModules.Role, "更新系统菜单")]
public sealed record UpdateMenuCommand(long Id, UpdateMenuRequest Request) : ICommand<MenuDto>;
