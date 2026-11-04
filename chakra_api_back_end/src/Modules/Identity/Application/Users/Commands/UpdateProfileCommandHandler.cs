using Identity.Application.Users.DTOs;
using Identity.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using SharedKernel.Application.CQRS;
using SharedKernel.Application.Exceptions;

namespace Identity.Application.Users.Commands;

/// <summary>
/// 更新个人资料命令处理器
/// </summary>
public sealed class UpdateProfileCommandHandler : ICommandHandler<UpdateProfileCommand, CurrentUserDto>
{
    private readonly IdentityDbContext _dbContext;

    public UpdateProfileCommandHandler(IdentityDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<CurrentUserDto> Handle(UpdateProfileCommand command, CancellationToken cancellationToken)
    {
        var user = await _dbContext.Users
            .FirstOrDefaultAsync(u => u.Id == command.UserId, cancellationToken);

        if (user == null)
        {
            throw new NotFoundException("User", command.UserId);
        }

        var req = command.Request;
        var normalizedEmail = req.Email?.Trim().ToLowerInvariant();

        // 邮箱变动时查重
        if (!string.IsNullOrWhiteSpace(normalizedEmail) && normalizedEmail != user.Email)
        {
            var emailExists = await _dbContext.Users
                .AnyAsync(u => u.Email == normalizedEmail && u.Id != command.UserId, cancellationToken);

            if (emailExists)
            {
                throw new BusinessException("该电子邮箱已被其他账号使用", 400);
            }
        }

        user.UpdateProfile(req.Nickname, normalizedEmail, req.AvatarUrl);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return CurrentUserDto.FromUser(user);
    }
}
