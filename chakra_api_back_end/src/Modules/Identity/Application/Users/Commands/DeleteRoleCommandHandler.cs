using Identity.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using SharedKernel.Application.CQRS;
using SharedKernel.Application.Exceptions;
using SharedKernel.Constants;

namespace Identity.Application.Users.Commands;

/// <summary>
/// 删除系统角色处理器
/// </summary>
public sealed class DeleteRoleCommandHandler : ICommandHandler<DeleteRoleCommand>
{
    private readonly IdentityDbContext _dbContext;

    public DeleteRoleCommandHandler(IdentityDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task Handle(DeleteRoleCommand command, CancellationToken cancellationToken)
    {
        var role = await _dbContext.Roles
            .FirstOrDefaultAsync(r => r.Id == command.Id && !r.IsDeleted, cancellationToken);

        if (role == null)
        {
            throw new NotFoundException("Role", command.Id);
        }

        // 1. 防死锁保护：系统内置保留角色严禁删除
        if (role.IsSystem || string.Equals(role.Code, RoleNames.Admin, StringComparison.OrdinalIgnoreCase))
        {
            throw new BusinessException($"系统内置保留角色 [{role.Name}] 受核心安全保护，不可删除", 400);
        }

        // 2. 检查是否仍有用户正在使用该角色
        var hasUsers = await _dbContext.Users
            .AnyAsync(u => u.Role == role.Code && !u.IsDeleted, cancellationToken);

        if (hasUsers)
        {
            throw new BusinessException($"角色 [{role.Name}] 仍被系统中的用户使用，请先将对应用户的角色调整后再尝试删除", 400);
        }

        role.MarkAsDeleted();
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
