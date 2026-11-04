using Identity.Domain.Events;
using Identity.Infrastructure.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Identity.Application.Users.Events;

/// <summary>
/// 用户安全属性变化后撤销其全部活动刷新令牌
/// </summary>
public sealed class UserSessionInvalidatedDomainEventHandler(
    IdentityDbContext dbContext,
    ILogger<UserSessionInvalidatedDomainEventHandler> logger)
    : INotificationHandler<UserSessionInvalidatedDomainEvent>
{
    /// <inheritdoc />
    public async Task Handle(UserSessionInvalidatedDomainEvent notification, CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var affectedRows = await dbContext.RefreshTokens
            .Where(token => token.UserId == notification.UserId && !token.IsRevoked)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(token => token.IsRevoked, true)
                    .SetProperty(token => token.RevokedAt, now)
                    .SetProperty(token => token.UpdatedAt, now)
                    .SetProperty(token => token.Version, token => token.Version + 1),
                cancellationToken);

        logger.LogInformation(
            "用户 {UserId} 因 {Reason} 失效 {SessionCount} 个刷新令牌会话",
            notification.UserId,
            notification.Reason,
            affectedRows);
    }
}
