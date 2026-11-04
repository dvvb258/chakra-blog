using Identity.Domain.Entities;
using Identity.Infrastructure.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SharedKernel.Application.CQRS;
using SharedKernel.Application.Exceptions;

namespace Identity.Application.Users.Commands;

/// <summary>
/// 修改密码命令处理器
/// </summary>
public sealed class ChangePasswordCommandHandler : ICommandHandler<ChangePasswordCommand, string>
{
    private readonly IdentityDbContext _dbContext;
    private readonly IPasswordHasher<User> _passwordHasher;

    public ChangePasswordCommandHandler(
        IdentityDbContext dbContext,
        IPasswordHasher<User> passwordHasher)
    {
        _dbContext = dbContext;
        _passwordHasher = passwordHasher;
    }

    public async Task<string> Handle(ChangePasswordCommand command, CancellationToken cancellationToken)
    {
        var user = await _dbContext.Users
            .FirstOrDefaultAsync(u => u.Id == command.UserId, cancellationToken);

        if (user == null)
        {
            throw new NotFoundException("User", command.UserId);
        }

        var req = command.Request;

        // 若用户已设置过密码，则必须校验原密码
        if (user.HasPassword)
        {
            if (string.IsNullOrWhiteSpace(req.OldPassword))
            {
                throw new BusinessException("请输入原登录密码进行核验", 400);
            }

            if (!user.VerifyPassword(req.OldPassword, _passwordHasher))
            {
                throw new BusinessException("原密码错误，核验未通过", 400);
            }
        }

        user.SetPassword(req.NewPassword, _passwordHasher);
        await _dbContext.SaveChangesAsync(cancellationToken);

        // 密码修改成功后，吊销该用户所有现存活跃的 RefreshToken，强制旧设备重新登录
        await _dbContext.RefreshTokens
            .Where(t => t.UserId == user.Id && !t.IsRevoked)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(t => t.IsRevoked, true)
                    .SetProperty(t => t.RevokedAt, DateTime.UtcNow)
                    .SetProperty(t => t.UpdatedAt, DateTime.UtcNow),
                cancellationToken);

        return "密码修改成功";
    }
}
