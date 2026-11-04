using Identity.Application.Common;
using Identity.Application.Common.Interfaces;
using Identity.Application.Users.DTOs;
using Identity.Domain.Entities;
using Identity.Infrastructure.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SharedKernel.Application.CQRS;
using SharedKernel.Application.Exceptions;

namespace Identity.Application.Users.Commands;

/// <summary>
/// 登录命令处理器
/// </summary>
public sealed class LoginCommandHandler : ICommandHandler<LoginCommand, AuthTokenDto>
{
    private readonly IdentityDbContext _dbContext;
    private readonly IPasswordHasher<User> _passwordHasher;
    private readonly IJwtTokenService _jwtTokenService;

    /// <summary>
    /// Initializes a new instance of the <see cref="LoginCommandHandler"/> class.
    /// </summary>
    /// <param name="dbContext">身份认证数据库上下文</param>
    /// <param name="passwordHasher">密码哈希器</param>
    /// <param name="jwtTokenService">JWT 服务</param>
    public LoginCommandHandler(
        IdentityDbContext dbContext,
        IPasswordHasher<User> passwordHasher,
        IJwtTokenService jwtTokenService)
    {
        _dbContext = dbContext;
        _passwordHasher = passwordHasher;
        _jwtTokenService = jwtTokenService;
    }

    /// <inheritdoc />
    public async Task<AuthTokenDto> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        var account = request.Account.Trim();
        var normalizedAccount = account.ToLowerInvariant();

        // 1. 查找用户：通用支持通过用户名或邮箱登录（底层入库均已通过 ToLowerInvariant 规范化存储，直接比对可命中原生 B-Tree 索引）
        var user = await _dbContext.Users
            .FirstOrDefaultAsync(
                u => u.Username == normalizedAccount || (u.Email != null && u.Email == normalizedAccount),
                cancellationToken);

        if (user is null)
        {
            throw new BusinessException("账号或密码错误", 401);
        }

        if (!user.IsActive)
        {
            throw new BusinessException("当前账号已被禁用，请联系管理员", 403);
        }

        // 2. 校验密码
        if (!user.VerifyPassword(request.Password, _passwordHasher))
        {
            throw new BusinessException("账号或密码错误", 401);
        }

        // 3. 记录登录时间
        user.RecordLogin();

        // 4. 生成双 Token（AccessToken + RefreshToken）
        var accessToken = _jwtTokenService.GenerateAccessToken(user);
        var refreshTokenStr = _jwtTokenService.GenerateRefreshTokenString();
        var refreshToken = new RefreshToken(
            user.Id,
            _jwtTokenService.HashRefreshToken(refreshTokenStr),
            _jwtTokenService.GetRefreshTokenExpiration(),
            request.IpAddress);

        _dbContext.RefreshTokens.Add(refreshToken);
        await _dbContext.SaveChangesAsync(cancellationToken);

        var (menus, permissions) = await _dbContext.GetUserMenusAndPermissionsAsync(user.Role, cancellationToken);

        return new AuthTokenDto
        {
            AccessToken = accessToken,
            RefreshToken = refreshTokenStr,
            TokenType = "Bearer",
            ExpiresIn = _jwtTokenService.GetAccessTokenExpirationSeconds(),
            User = CurrentUserDto.FromUser(user, menus, permissions)
        };
    }
}
