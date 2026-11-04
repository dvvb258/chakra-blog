using Identity.Application.Users.DTOs;
using SharedKernel.Application.Attributes;
using SharedKernel.Application.CQRS;
using SharedKernel.Constants;

namespace Identity.Application.Users.Commands;

/// <summary>
/// 创建角色命令
/// </summary>
/// <param name="Request">创建角色参数</param>
[AuditLog(AuditModules.Role, "创建系统角色")]
public sealed record CreateRoleCommand(CreateRoleRequest Request) : ICommand<RoleDto>;
