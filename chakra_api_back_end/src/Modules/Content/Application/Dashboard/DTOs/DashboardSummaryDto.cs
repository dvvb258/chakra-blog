namespace Content.Application.Dashboard.DTOs;

/// <summary>
/// 仪表盘聚合汇总数据 DTO
/// </summary>
public record DashboardSummaryDto
{
    /// <summary>全站总访问量（Redis）</summary>
    public long TotalVisits { get; init; }

    /// <summary>收录文章总数</summary>
    public int TotalArticles { get; init; }

    /// <summary>文章累计总阅读量</summary>
    public long TotalViews { get; init; }

    /// <summary>专栏系列总数</summary>
    public int TotalSeries { get; init; }

    /// <summary>各分类文章数量分布</summary>
    public List<CategoryStatDto> CategoryDistribution { get; init; } = [];

    /// <summary>最新发布的文章列表（Top 5）</summary>
    public List<RecentArticleDto> RecentArticles { get; init; } = [];

    /// <summary>近7天活跃度趋势数据</summary>
    public List<TrendItemDto> TrendData { get; init; } = [];
}
