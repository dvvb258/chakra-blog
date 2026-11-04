using Content.Application.Articles.DTOs;
using SharedKernel.Application;
using SharedKernel.Application.CQRS;

namespace Content.Application.Articles.Queries;

/// <summary>
/// 获取已发布文章列表查询
/// </summary>
public sealed record GetPublishedArticlesQuery : IQuery<PagedResult<ArticleListItemDto>>
{
    private int _pageIndex = 1;
    private int _pageSize = 10;

    /// <summary>
    /// 关键词（搜索标题）
    /// </summary>
    public string? Keyword { get; init; }

    /// <summary>
    /// 分类ID筛选
    /// </summary>
    public long? CategoryId { get; init; }

    /// <summary>
    /// 作者筛选
    /// </summary>
    public string? Author { get; init; }

    /// <summary>
    /// 标签筛选
    /// </summary>
    public string? Tag { get; init; }

    /// <summary>
    /// 页码
    /// </summary>
    public int PageIndex
    {
        get => _pageIndex;
        init => _pageIndex = value < 1 ? 1 : value;
    }

    /// <summary>
    /// 每页大小
    /// </summary>
    public int PageSize
    {
        get => _pageSize;
        init => _pageSize = value < 1 ? 10 : (value > 100 ? 100 : value);
    }
}
