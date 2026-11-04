using Identity.Application.Common.Interfaces;
using Identity.Application.Users.DTOs;
using Identity.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using SharedKernel.Application.CQRS;
using SharedKernel.Application.Exceptions;

namespace Identity.Application.Users.Commands;

/// <summary>
/// 绑定 GitHub 账号命令处理器
/// </summary>
public sealed class BindGithubCommandHandler : ICommandHandler<BindGithubCommand, CurrentUserDto>
{
    private readonly IdentityDbContext _dbContext;
    private readonly IGitHubOAuthService _gitHubOAuthService;

    public BindGithubCommandHandler(
        IdentityDbContext dbContext,
        IGitHubOAuthService gitHubOAuthService)
    {
        _dbContext = dbContext;
        _gitHubOAuthService = gitHubOAuthService;
    }

    public async Task<CurrentUserDto> Handle(BindGithubCommand command, CancellationToken cancellationToken)
    {
        var user = await _dbContext.Users
            .FirstOrDefaultAsync(u => u.Id == command.UserId, cancellationToken);

        if (user == null)
        {
            throw new NotFoundException("User", command.UserId);
        }

        // 通过授权码换取 GitHub 个人资料
        var githubUser = await _gitHubOAuthService.GetUserInfoByCodeAsync(command.Code, cancellationToken);

        // 检查该 GitHub ID 是否已被其他账号绑定
        var existingUser = await _dbContext.Users
            .FirstOrDefaultAsync(u => u.GithubId == githubUser.Id && u.Id != command.UserId, cancellationToken);

        if (existingUser != null)
        {
            throw new BusinessException($"该 GitHub 账号已绑定至其他用户 [{existingUser.Username}]", 400);
        }

        user.BindGithub(githubUser.Id, githubUser.AvatarUrl, githubUser.Name ?? githubUser.Login);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return CurrentUserDto.FromUser(user);
    }
}
