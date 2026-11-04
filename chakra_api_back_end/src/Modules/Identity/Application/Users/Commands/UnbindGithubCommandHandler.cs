using Identity.Application.Users.DTOs;
using Identity.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using SharedKernel.Application.CQRS;
using SharedKernel.Application.Exceptions;

namespace Identity.Application.Users.Commands;

/// <summary>
/// 解绑 GitHub 账号命令处理器
/// </summary>
public sealed class UnbindGithubCommandHandler : ICommandHandler<UnbindGithubCommand, CurrentUserDto>
{
    private readonly IdentityDbContext _dbContext;

    public UnbindGithubCommandHandler(IdentityDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<CurrentUserDto> Handle(UnbindGithubCommand command, CancellationToken cancellationToken)
    {
        var user = await _dbContext.Users
            .FirstOrDefaultAsync(u => u.Id == command.UserId, cancellationToken);

        if (user == null)
        {
            throw new NotFoundException("User", command.UserId);
        }

        if (!user.HasPassword)
        {
            throw new BusinessException("当前账号尚未设置本地登录密码，解绑 GitHub 后将无法登录系统，请先设置登录密码！", 400);
        }

        user.UnbindGithub();
        await _dbContext.SaveChangesAsync(cancellationToken);

        return CurrentUserDto.FromUser(user);
    }
}
