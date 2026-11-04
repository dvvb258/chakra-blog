using Identity.Application.Users.DTOs;
using SharedKernel.Application.Attributes;
using SharedKernel.Application.CQRS;
using SharedKernel.Constants;

namespace Identity.Application.Users.Commands;

/// <summary>
/// 创建系统菜单命令
/// </summary>
/// <param name="Request">创建系统菜单参数</param>
[AuditLog(AuditModules.Role, "创建系统菜单")]
public sealed record CreateMenuCommand(CreateMenuRequest Request) : ICommand<MenuDto>;
