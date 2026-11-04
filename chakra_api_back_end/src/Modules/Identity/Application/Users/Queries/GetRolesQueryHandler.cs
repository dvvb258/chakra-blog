using Identity.Application.Users.DTOs;
using Identity.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using SharedKernel.Application.CQRS;
using SharedKernel.Constants;

namespace Identity.Application.Users.Queries;

/// <summary>
/// 查询系统可用角色列表处理器
/// </summary>
public sealed class GetRolesQueryHandler : IQueryHandler<GetRolesQuery, List<RoleDto>>
{
    private readonly IdentityDbContext _dbContext;

    /// <summary>
    /// Initializes a new instance of the <see cref="GetRolesQueryHandler"/> class.
    /// </summary>
    /// <param name="dbContext">身份认证数据库上下文</param>
    public GetRolesQueryHandler(IdentityDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    /// <inheritdoc />
    public async Task<List<RoleDto>> Handle(GetRolesQuery request, CancellationToken cancellationToken)
    {
        var roles = await _dbContext.Roles
            .AsNoTracking()
            .Where(r => !r.IsDeleted)
            .OrderBy(r => r.Id)
            .ToListAsync(cancellationToken);

        var roleMenus = await _dbContext.RoleMenus
            .AsNoTracking()
            .Where(rm => !rm.IsDeleted)
            .ToListAsync(cancellationToken);

        var allMenus = await _dbContext.Menus
            .AsNoTracking()
            .Where(m => !m.IsDeleted)
            .Select(m => m.Id)
            .ToListAsync(cancellationToken);

        var result = roles.Select(r =>
        {
            List<long> menuIds;

            // 核心法则：超级管理员在底层天然具备全量权限菜单
            if (string.Equals(r.Code, RoleNames.Admin, StringComparison.OrdinalIgnoreCase))
            {
                menuIds = allMenus;
            }
            else
            {
                menuIds = roleMenus.Where(rm => rm.RoleId == r.Id).Select(rm => rm.MenuId).ToList();
            }

            return new RoleDto
            {
                Id = r.Id,
                Code = r.Code,
                Name = r.Name,
                Description = r.Description,
                IsSystem = r.IsSystem || string.Equals(r.Code, RoleNames.Admin, StringComparison.OrdinalIgnoreCase),
                MenuIds = menuIds,
            };
        }).ToList();

        return result;
    }
}
