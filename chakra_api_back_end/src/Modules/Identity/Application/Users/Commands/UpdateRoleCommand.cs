using Identity.Application.Users.DTOs;
using SharedKernel.Application.Attributes;
using SharedKernel.Application.CQRS;
using SharedKernel.Constants;

namespace Identity.Application.Users.Commands;

/// <summary>
/// 更新角色及其菜单权限命令
/// </summary>
/// <param name="Id">角色ID</param>
/// <param name="Request">更新参数</param>
[AuditLog(AuditModules.Role, "更新角色权限")]
public sealed record UpdateRoleCommand(long Id, UpdateRoleRequest Request) : ICommand<RoleDto>;
