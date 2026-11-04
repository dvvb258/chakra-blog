using Identity.Application.Users.DTOs;
using SharedKernel.Application.CQRS;

namespace Identity.Application.Users.Queries;

/// <summary>
/// 获取当前登录用户可访问的菜单列表查询
/// </summary>
/// <param name="UserId">当前用户ID</param>
public sealed record GetMyMenusQuery(long UserId) : IQuery<List<MenuDto>>;
