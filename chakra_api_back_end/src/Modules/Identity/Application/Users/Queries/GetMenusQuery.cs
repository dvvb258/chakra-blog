using Identity.Application.Users.DTOs;
using SharedKernel.Application.CQRS;

namespace Identity.Application.Users.Queries;

/// <summary>
/// 获取全量系统菜单列表查询
/// </summary>
public sealed record GetMenusQuery : IQuery<List<MenuDto>>;
