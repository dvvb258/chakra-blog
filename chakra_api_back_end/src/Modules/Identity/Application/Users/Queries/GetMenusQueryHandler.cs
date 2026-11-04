using Identity.Application.Users.DTOs;
using Identity.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using SharedKernel.Application.CQRS;

namespace Identity.Application.Users.Queries;

/// <summary>
/// 获取全量系统菜单列表查询处理器
/// </summary>
public sealed class GetMenusQueryHandler : IQueryHandler<GetMenusQuery, List<MenuDto>>
{
    private readonly IdentityDbContext _dbContext;

    public GetMenusQueryHandler(IdentityDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<List<MenuDto>> Handle(GetMenusQuery request, CancellationToken cancellationToken)
    {
        var menus = await _dbContext.Menus
            .AsNoTracking()
            .Where(m => !m.IsDeleted)
            .OrderBy(m => m.Sort)
            .Select(m => new MenuDto
            {
                Id = m.Id,
                ParentId = m.ParentId,
                Name = m.Name,
                Path = m.Path,
                PermissionCode = m.PermissionCode,
                Icon = m.Icon,
                Sort = m.Sort,
                IsHidden = m.IsHidden,
                Type = (int)m.Type,
            })
            .ToListAsync(cancellationToken);

        return menus;
    }
}
