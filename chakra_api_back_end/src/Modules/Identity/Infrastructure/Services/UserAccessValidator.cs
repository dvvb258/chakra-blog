using Identity.Application.Common.Interfaces;
using Identity.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Identity.Infrastructure.Services;

/// <summary>
/// 基于身份数据库校验访问令牌中的用户状态与角色
/// </summary>
public sealed class UserAccessValidator(IdentityDbContext dbContext) : IUserAccessValidator
{
    /// <inheritdoc />
    public Task<bool> IsAccessAllowedAsync(long userId, string role, CancellationToken cancellationToken = default)
    {
        if (userId <= 0 || string.IsNullOrWhiteSpace(role))
        {
            return Task.FromResult(false);
        }

        var normalizedRole = role.Trim().ToLowerInvariant();
        return dbContext.Users
            .AsNoTracking()
            .AnyAsync(
                user => user.Id == userId &&
                        user.IsActive &&
                        user.Role.ToLower() == normalizedRole,
                cancellationToken);
    }
}
