using Identity.Application.Common;
using Identity.Application.Common.Interfaces;
using Identity.Application.Users.DTOs;
using Identity.Domain.Entities;
using Identity.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using SharedKernel.Application.CQRS;
using SharedKernel.Application.Exceptions;

namespace Identity.Application.Users.Commands;

/// <summary>
/// 刷新令牌命令处理器（支持令牌轮换与防重放）
/// </summary>
public sealed class RefreshTokenCommandHandler : ICommandHandler<RefreshTokenCommand, AuthTokenDto>
{
    private readonly IdentityDbContext _dbContext;
    private readonly IJwtTokenService _jwtTokenService;

    /// <summary>
    /// Initializes a new instance of the <see cref="RefreshTokenCommandHandler"/> class.
    /// </summary>
    /// <param name="dbContext">身份认证数据库上下文</param>
    /// <param name="jwtTokenService">JWT 服务</param>
    public RefreshTokenCommandHandler(
        IdentityDbContext dbContext,
        IJwtTokenService jwtTokenService)
    {
        _dbContext = dbContext;
        _jwtTokenService = jwtTokenService;
    }

    /// <inheritdoc />
    public async Task<AuthTokenDto> Handle(RefreshTokenCommand request, CancellationToken cancellationToken)
    {
        var rawToken = request.RefreshToken.Trim();
        var tokenHash = _jwtTokenService.HashRefreshToken(rawToken);
        var now = DateTime.UtcNow;

        // 1. 只读取令牌归属，最终有效性由后续条件更新原子裁决
        var existingToken = await _dbContext.RefreshTokens
            .AsNoTracking()
            .Where(t => t.TokenHash == tokenHash && !t.IsRevoked && t.ExpiresAt > now)
            .Select(t => new { t.Id, t.UserId })
            .FirstOrDefaultAsync(cancellationToken);

        if (existingToken is null)
        {
            throw new BusinessException("刷新令牌无效或已过期，请重新登录", 401);
        }

        // 2. 验证关联用户
        var user = await _dbContext.Users
            .FirstOrDefaultAsync(u => u.Id == existingToken.UserId, cancellationToken);

        if (user is null || !user.IsActive)
        {
            throw new BusinessException("账号不存在或已被停用", 403);
        }

        // 3. 执行令牌轮换：条件更新保证同一旧令牌并发请求中只有一个成功
        var newRefreshTokenStr = _jwtTokenService.GenerateRefreshTokenString();
        var newRefreshTokenHash = _jwtTokenService.HashRefreshToken(newRefreshTokenStr);

        await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

        var affectedRows = await _dbContext.RefreshTokens
            .Where(t => t.Id == existingToken.Id && !t.IsRevoked && t.ExpiresAt > now)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(t => t.IsRevoked, true)
                    .SetProperty(t => t.RevokedAt, now)
                    .SetProperty(t => t.ReplacedByTokenHash, newRefreshTokenHash)
                    .SetProperty(t => t.UpdatedAt, now),
                cancellationToken);

        if (affectedRows != 1)
        {
            throw new BusinessException("刷新令牌已被使用，请重新登录", 401);
        }

        var newRefreshToken = new RefreshToken(
            user.Id,
            newRefreshTokenHash,
            _jwtTokenService.GetRefreshTokenExpiration(),
            request.IpAddress);

        _dbContext.RefreshTokens.Add(newRefreshToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        // 4. 生成新 AccessToken
        var newAccessToken = _jwtTokenService.GenerateAccessToken(user);

        var (menus, permissions) = await _dbContext.GetUserMenusAndPermissionsAsync(user.Role, cancellationToken);

        return new AuthTokenDto
        {
            AccessToken = newAccessToken,
            RefreshToken = newRefreshTokenStr,
            TokenType = "Bearer",
            ExpiresIn = _jwtTokenService.GetAccessTokenExpirationSeconds(),
            User = CurrentUserDto.FromUser(user, menus, permissions)
        };
    }
}
