using Identity.Application.Users.DTOs;
using SharedKernel.Application.CQRS;

namespace Identity.Application.Users.Queries;

/// <summary>
/// 查询系统可用角色列表
/// </summary>
public sealed record GetRolesQuery : IQuery<List<RoleDto>>;
