using Content.Application.Series.DTOs;
using SharedKernel.Application;
using SharedKernel.Application.CQRS;

namespace Content.Application.Series.Queries;

/// <summary>
/// 获取系列列表查询
/// </summary>
public sealed record GetSeriesListQuery : IQuery<PagedResult<SeriesListItemDto>>
{
    private int _pageIndex = 1;
    private int _pageSize = 10;

    /// <summary>关键词（搜索名称）</summary>
    public string? Keyword { get; init; }

    /// <summary>作者筛选</summary>
    public string? Author { get; init; }

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
