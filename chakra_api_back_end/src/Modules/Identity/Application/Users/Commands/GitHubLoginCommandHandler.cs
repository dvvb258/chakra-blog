using Identity.Application.Common;
using Identity.Application.Common.Interfaces;
using Identity.Application.Users.DTOs;
using Identity.Domain.Entities;
using Identity.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SharedKernel.Application.CQRS;
using SharedKernel.Application.Exceptions;
using SharedKernel.Constants;

namespace Identity.Application.Users.Commands;

/// <summary>
/// GitHub OAuth 登录命令处理器
/// </summary>
public sealed class GitHubLoginCommandHandler : ICommandHandler<GitHubLoginCommand, AuthTokenDto>
{
    private readonly IdentityDbContext _dbContext;
    private readonly IGitHubOAuthService _gitHubOAuthService;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly ILogger<GitHubLoginCommandHandler> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="GitHubLoginCommandHandler"/> class.
    /// </summary>
    /// <param name="dbContext">身份数据库上下文</param>
    /// <param name="gitHubOAuthService">GitHub 开放平台通信服务</param>
    /// <param name="jwtTokenService">JWT 令牌签发服务</param>
    /// <param name="logger">日志记录器</param>
    public GitHubLoginCommandHandler(
        IdentityDbContext dbContext,
        IGitHubOAuthService gitHubOAuthService,
        IJwtTokenService jwtTokenService,
        ILogger<GitHubLoginCommandHandler> logger)
    {
        _dbContext = dbContext;
        _gitHubOAuthService = gitHubOAuthService;
        _jwtTokenService = jwtTokenService;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<AuthTokenDto> Handle(GitHubLoginCommand request, CancellationToken cancellationToken)
    {
        // 1. 通过 GitHub Code 换取用户信息
        var githubUser = await _gitHubOAuthService.GetUserInfoByCodeAsync(request.Code, cancellationToken);
        _logger.LogInformation("收到 GitHub 登录请求，用户: {Login}, ID: {GithubId}, Email: {Email}", githubUser.Login, githubUser.Id, githubUser.Email);

        // 验证邮箱是否存在
        if (string.IsNullOrWhiteSpace(githubUser.Email))
        {
            throw new BusinessException("GitHub 账号未提供有效邮箱或未公开邮箱，系统除超级管理员外统一以邮箱作为用户标识，无法登录", 400);
        }

        var normalizedEmail = githubUser.Email.Trim().ToLowerInvariant();

        // 2. 检查系统中是否已有用户绑定此 GitHub ID
        var user = await _dbContext.Users
            .FirstOrDefaultAsync(u => u.GithubId == githubUser.Id, cancellationToken);

        if (user != null)
        {
            // 超级管理员账号绝对禁止第三方登录
            if (user.Username == "admin")
            {
                throw new BusinessException("系统超级管理员禁止通过第三方平台登录", 403);
            }

            // 检查账号状态
            if (!user.IsActive)
            {
                throw new BusinessException("当前绑定的系统账号已被禁用，无法登录", 403);
            }

            // 补充更新头像/昵称
            var safeAvatar = githubUser.AvatarUrl?.Length > 500 ? githubUser.AvatarUrl[..500] : githubUser.AvatarUrl;
            if (string.IsNullOrWhiteSpace(user.AvatarUrl) && !string.IsNullOrWhiteSpace(safeAvatar))
            {
                user.UpdateProfile(user.Nickname, user.Email, safeAvatar);
            }
        }
        else
        {
            // 该 GitHub ID 尚未绑定任何系统账号：
            // 1. 安全防范：检查系统中是否已存在使用该邮箱的已有账号。
            // 若已存在，禁止静默自动绑定（防止账号接管越权风险），提示用户先使用密码登录再前往个人中心手动绑定。
            var existingUserWithEmail = await _dbContext.Users
                .FirstOrDefaultAsync(u => u.Email == normalizedEmail, cancellationToken);

            if (existingUserWithEmail != null)
            {
                _logger.LogWarning(
                    "GitHub 用户 [{Login}] (ID: {GithubId}) 尝试登录，但邮箱 [{Email}] 已被系统账号 [{Username}] 使用",
                    githubUser.Login,
                    githubUser.Id,
                    normalizedEmail,
                    existingUserWithEmail.Username);

                throw new BusinessException(
                    $"系统已存在使用邮箱 [{normalizedEmail}] 的账号。为保障账号安全，请先使用密码登录，再前往【个人设置】中手动绑定此 GitHub 账号。",
                    400);
            }

            // 2. 该邮箱在系统中未被占用，直接为该 GitHub 用户创建新系统账号（默认 Editor 角色）
            var rawName = string.IsNullOrWhiteSpace(githubUser.Name) ? githubUser.Login : githubUser.Name;
            var displayName = rawName.Length > 50 ? rawName[..50] : rawName;
            var safeAvatar = githubUser.AvatarUrl?.Length > 500 ? githubUser.AvatarUrl[..500] : githubUser.AvatarUrl;

            // 用户名优先使用 GitHub Login，若已被占用则采用 github_{githubId} 避免重名
            var rawLogin = githubUser.Login.Trim().ToLowerInvariant();
            var candidateUsername = rawLogin.Length > 50 ? rawLogin[..50] : rawLogin;
            var usernameExists = await _dbContext.Users
                .IgnoreQueryFilters()
                .AnyAsync(u => u.Username == candidateUsername, cancellationToken);
            var finalUsername = usernameExists ? $"github_{githubUser.Id}" : candidateUsername;

            user = new User(
                username: finalUsername,
                nickname: displayName,
                role: RoleNames.Editor,
                email: normalizedEmail,
                avatarUrl: safeAvatar);

            user.BindGithub(githubUser.Id, safeAvatar, displayName);
            _dbContext.Users.Add(user);

            // 关键：新创建的用户必须先行落库，确保 user.id 在主表完全持久化，避免外键依赖排序冲突
            await _dbContext.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "已为新 GitHub 用户 [{Login}] 成功创建系统账号 [{Username}] (邮箱: {Email})",
                githubUser.Login,
                finalUsername,
                normalizedEmail);
        }

        // 3. 记录登录时间
        user.RecordLogin();

        // 4. 生成双 Token（AccessToken + RefreshToken）
        var accessToken = _jwtTokenService.GenerateAccessToken(user);
        var refreshTokenStr = _jwtTokenService.GenerateRefreshTokenString();
        var safeIp = request.IpAddress?.Length > 50 ? request.IpAddress[..50] : request.IpAddress;
        var refreshToken = new RefreshToken(
            user.Id,
            _jwtTokenService.HashRefreshToken(refreshTokenStr),
            _jwtTokenService.GetRefreshTokenExpiration(),
            safeIp);

        _dbContext.RefreshTokens.Add(refreshToken);
        await _dbContext.SaveChangesAsync(cancellationToken);

        var (menus, permissions) = await _dbContext.GetUserMenusAndPermissionsAsync(user.Role, cancellationToken);

        return new AuthTokenDto
        {
            AccessToken = accessToken,
            RefreshToken = refreshTokenStr,
            TokenType = "Bearer",
            ExpiresIn = _jwtTokenService.GetAccessTokenExpirationSeconds(),
            User = CurrentUserDto.FromUser(user, menus, permissions),
        };
    }
}
