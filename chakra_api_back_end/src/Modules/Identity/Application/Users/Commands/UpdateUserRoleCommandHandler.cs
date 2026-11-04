using Identity.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using SharedKernel.Application.CQRS;
using SharedKernel.Application.Exceptions;
using SharedKernel.Constants;

namespace Identity.Application.Users.Commands;

/// <summary>
/// 更新用户角色命令处理器
/// </summary>
public sealed class UpdateUserRoleCommandHandler : ICommandHandler<UpdateUserRoleCommand>
{
    private readonly IdentityDbContext _dbContext;

    /// <summary>
    /// Initializes a new instance of the <see cref="UpdateUserRoleCommandHandler"/> class.
    /// </summary>
    /// <param name="dbContext">身份认证数据库上下文</param>
    public UpdateUserRoleCommandHandler(IdentityDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    /// <inheritdoc />
    public async Task Handle(UpdateUserRoleCommand command, CancellationToken cancellationToken)
    {
        var user = await _dbContext.Users
            .FirstOrDefaultAsync(u => u.Id == command.UserId, cancellationToken);

        if (user == null)
        {
            throw new NotFoundException("User", command.UserId);
        }

        if (user.Username == "admin" && command.Role != RoleNames.Admin)
        {
            throw new BusinessException("系统默认超级管理员账号角色不可修改", 400);
        }

        var targetRole = command.Role.Trim();
        if (targetRole != RoleNames.Admin && targetRole != RoleNames.Editor)
        {
            throw new BusinessException($"目标角色非法，仅支持 {RoleNames.Admin} 或 {RoleNames.Editor}", 400);
        }

        user.UpdateRole(targetRole);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
