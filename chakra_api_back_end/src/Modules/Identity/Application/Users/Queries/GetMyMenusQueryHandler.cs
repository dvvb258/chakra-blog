using Identity.Application.Common.Interfaces;
using Identity.Application.Common.Models;
using Identity.Application.Users.DTOs;
using Identity.Domain.Enums;
using SharedKernel.Application.CQRS;
using SharedKernel.Constants;

namespace Identity.Application.Users.Queries;

/// <summary>
/// 获取当前用户可访问菜单处理器
/// </summary>
public sealed class GetMyMenusQueryHandler : IQueryHandler<GetMyMenusQuery, List<MenuDto>>
{
    private readonly IIdentityReadStore _readStore;

    /// <summary>
    /// 初始化查询处理器
    /// </summary>
    /// <param name="readStore">Identity 读模型存储</param>
    public GetMyMenusQueryHandler(IIdentityReadStore readStore)
    {
        _readStore = readStore;
    }

    /// <inheritdoc />
    public async Task<List<MenuDto>> Handle(GetMyMenusQuery request, CancellationToken cancellationToken)
    {
        var role = await _readStore.GetActiveUserRoleAsync(request.UserId, cancellationToken);

        if (role is null)
        {
            return [];
        }

        // 核心法则：超级管理员拥有通配全量特权，零配置获取系统所有菜单
        IReadOnlyList<MenuRow> menus = string.Equals(role, RoleNames.Admin, StringComparison.OrdinalIgnoreCase)
            ? await _readStore.GetActiveMenusAsync(cancellationToken)
            : await _readStore.GetMenusByRoleCodeAsync(role, cancellationToken);

        return menus
            .Where(m => m.Type != (int)MenuType.Button)
            .Select(m => m.ToDto())
            .ToList();
    }
}
