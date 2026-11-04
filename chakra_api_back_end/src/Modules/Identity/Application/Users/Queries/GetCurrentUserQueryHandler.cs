using Identity.Application.Common;
using Identity.Application.Users.DTOs;
using Identity.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using SharedKernel.Application.CQRS;
using SharedKernel.Application.Exceptions;

namespace Identity.Application.Users.Queries;

/// <summary>
/// 获取当前登录用户信息查询处理器
/// </summary>
public sealed class GetCurrentUserQueryHandler : IQueryHandler<GetCurrentUserQuery, CurrentUserDto>
{
    private readonly IdentityDbContext _dbContext;

    /// <summary>
    /// Initializes a new instance of the <see cref="GetCurrentUserQueryHandler"/> class.
    /// </summary>
    /// <param name="dbContext">身份认证数据库上下文</param>
    public GetCurrentUserQueryHandler(IdentityDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    /// <inheritdoc />
    public async Task<CurrentUserDto> Handle(GetCurrentUserQuery request, CancellationToken cancellationToken)
    {
        var user = await _dbContext.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == request.UserId, cancellationToken);

        if (user is null)
        {
            throw new NotFoundException("User", request.UserId);
        }

        var (menuPaths, permissions) = await _dbContext.GetUserMenusAndPermissionsAsync(user.Role, cancellationToken);

        return new CurrentUserDto
        {
            Id = user.Id,
            Username = user.Username,
            Nickname = user.Nickname,
            Email = user.Email,
            AvatarUrl = user.AvatarUrl,
            Role = user.Role,
            GithubId = user.GithubId,
            LastLoginAt = user.LastLoginAt,
            HasPassword = user.HasPassword,
            Menus = menuPaths,
            Permissions = permissions,
        };
    }
}
