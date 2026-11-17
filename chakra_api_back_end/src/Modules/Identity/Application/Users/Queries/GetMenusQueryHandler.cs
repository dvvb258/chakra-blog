using Identity.Application.Common.Interfaces;
using Identity.Application.Users.DTOs;
using SharedKernel.Application.CQRS;

namespace Identity.Application.Users.Queries;

/// <summary>
/// 获取全量系统菜单列表查询处理器
/// </summary>
public sealed class GetMenusQueryHandler : IQueryHandler<GetMenusQuery, List<MenuDto>>
{
    private readonly IIdentityReadStore _readStore;

    public GetMenusQueryHandler(IIdentityReadStore readStore)
    {
        _readStore = readStore;
    }

    public async Task<List<MenuDto>> Handle(GetMenusQuery request, CancellationToken cancellationToken)
    {
        var menus = await _readStore.GetActiveMenusAsync(cancellationToken);

        return menus.Select(m => m.ToDto()).ToList();
    }
}
