using Identity.Application.Users.DTOs;
using SharedKernel.Application.CQRS;

namespace Identity.Application.Users.Queries;

/// <summary>
/// 获取当前登录用户信息查询
/// </summary>
/// <param name="UserId">当前登录用户ID</param>
public sealed record GetCurrentUserQuery(long UserId) : IQuery<CurrentUserDto>;
