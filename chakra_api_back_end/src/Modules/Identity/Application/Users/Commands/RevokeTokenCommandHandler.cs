using Identity.Application.Common.Interfaces;
using Identity.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using SharedKernel.Application.CQRS;

namespace Identity.Application.Users.Commands;

/// <summary>
/// 撤销刷新令牌命令处理器
/// </summary>
public sealed class RevokeTokenCommandHandler : ICommandHandler<RevokeTokenCommand>
{
    private readonly IdentityDbContext _dbContext;
    private readonly IJwtTokenService _jwtTokenService;

    /// <summary>
    /// Initializes a new instance of the <see cref="RevokeTokenCommandHandler"/> class.
    /// </summary>
    /// <param name="dbContext">身份认证数据库上下文</param>
    /// <param name="jwtTokenService">JWT 令牌服务</param>
    public RevokeTokenCommandHandler(IdentityDbContext dbContext, IJwtTokenService jwtTokenService)
    {
        _dbContext = dbContext;
        _jwtTokenService = jwtTokenService;
    }

    /// <inheritdoc />
    public async Task Handle(RevokeTokenCommand request, CancellationToken cancellationToken)
    {
        var rawToken = request.RefreshToken?.Trim();
        if (string.IsNullOrEmpty(rawToken))
        {
            return;
        }

        var tokenHash = _jwtTokenService.HashRefreshToken(rawToken);
        var now = DateTime.UtcNow;

        await _dbContext.RefreshTokens
            .Where(t => t.TokenHash == tokenHash && !t.IsRevoked)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(t => t.IsRevoked, true)
                    .SetProperty(t => t.RevokedAt, now)
                    .SetProperty(t => t.UpdatedAt, now),
                cancellationToken);
    }
}
