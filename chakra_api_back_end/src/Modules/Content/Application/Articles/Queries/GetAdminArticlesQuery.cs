using Content.Application.Articles.DTOs;
using SharedKernel.Application;
using SharedKernel.Application.CQRS;

namespace Content.Application.Articles.Queries;

/// <summary>
/// 管理端文章分页列表查询
/// </summary>
public sealed record GetAdminArticlesQuery : IQuery<PagedResult<ArticleListItemDto>>
{
    private int _pageIndex = 1;
    private int _pageSize = 10;

    /// <summary>
    /// 搜索关键词（匹配标题或摘要）
    /// </summary>
    public string? Keyword { get; init; }

    /// <summary>
    /// 分类ID筛选
    /// </summary>
    public long? CategoryId { get; init; }

    /// <summary>
    /// 标签筛选
    /// </summary>
    public string? Tag { get; init; }

    /// <summary>
    /// 归属专栏系列ID筛选
    /// </summary>
    public long? SeriesId { get; init; }

    /// <summary>
    /// 文章审核状态筛选（0: 草稿, 1: 已发布, 2: 待审核, 3: 已退回）
    /// </summary>
    public int? Status { get; init; }

    /// <summary>
    /// 创建人用户ID（Editor 身份将由系统强制锁定为当前登录人）
    /// </summary>
    public long? CreatedByUserId { get; set; }

    /// <summary>
    /// 页码
    /// </summary>
    public int PageIndex
    {
        get => _pageIndex;
        init => _pageIndex = value < 1 ? 1 : value;
    }

    /// <summary>
    /// 每页记录数
    /// </summary>
    public int PageSize
    {
        get => _pageSize;
        init => _pageSize = value < 1 ? 10 : (value > 100 ? 100 : value);
    }
}
