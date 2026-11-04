using Identity.Application.Users.DTOs;
using Identity.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using SharedKernel.Application;
using SharedKernel.Application.CQRS;

namespace Identity.Application.Users.Queries;

/// <summary>
/// 分页查询系统用户列表处理器
/// </summary>
public sealed class GetUsersQueryHandler : IQueryHandler<GetUsersQuery, PagedResult<UserListItemDto>>
{
    private readonly IdentityDbContext _dbContext;

    /// <summary>
    /// Initializes a new instance of the <see cref="GetUsersQueryHandler"/> class.
    /// </summary>
    /// <param name="dbContext">身份认证数据库上下文</param>
    public GetUsersQueryHandler(IdentityDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    /// <inheritdoc />
    public async Task<PagedResult<UserListItemDto>> Handle(GetUsersQuery request, CancellationToken cancellationToken)
    {
        var query = _dbContext.Users.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(request.Keyword))
        {
            var kw = request.Keyword.Trim();
            query = query.Where(u => u.Username.Contains(kw) || u.Nickname.Contains(kw) || (u.Email != null && u.Email.Contains(kw)));
        }

        if (!string.IsNullOrWhiteSpace(request.Role))
        {
            query = query.Where(u => u.Role == request.Role.Trim());
        }

        if (request.IsActive.HasValue)
        {
            query = query.Where(u => u.IsActive == request.IsActive.Value);
        }

        var total = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(u => u.CreatedAt)
            .Skip((request.PageIndex - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(u => new UserListItemDto
            {
                Id = u.Id,
                Username = u.Username,
                Nickname = u.Nickname,
                Email = u.Email,
                AvatarUrl = u.AvatarUrl,
                Role = u.Role,
                IsActive = u.IsActive,
                HasPassword = !string.IsNullOrEmpty(u.PasswordHash),
                GithubId = u.GithubId,
                LastLoginAt = u.LastLoginAt,
                CreatedAt = u.CreatedAt
            })
            .ToListAsync(cancellationToken);

        return new PagedResult<UserListItemDto>
        {
            Items = items,
            Total = total,
            PageIndex = request.PageIndex,
            PageSize = request.PageSize
        };
    }
}
