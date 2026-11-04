using Identity.Application.Users.DTOs;
using SharedKernel.Application;
using SharedKernel.Application.CQRS;

namespace Identity.Application.Users.Queries;

/// <summary>
/// 分页查询系统用户列表
/// </summary>
public sealed record GetUsersQuery : IQuery<PagedResult<UserListItemDto>>
{
    /// <summary>搜索关键词（用户名、昵称或邮箱）</summary>
    public string? Keyword { get; init; }

    /// <summary>按角色筛选（Admin 或 Editor）</summary>
    public string? Role { get; init; }

    /// <summary>按激活状态筛选</summary>
    public bool? IsActive { get; init; }

    private int _pageIndex = 1;
    private int _pageSize = 10;

    /// <summary>页码</summary>
    public int PageIndex
    {
        get => _pageIndex;
        init => _pageIndex = value < 1 ? 1 : value;
    }

    /// <summary>每页大小</summary>
    public int PageSize
    {
        get => _pageSize;
        init => _pageSize = value < 1 ? 10 : (value > 100 ? 100 : value);
    }
}
