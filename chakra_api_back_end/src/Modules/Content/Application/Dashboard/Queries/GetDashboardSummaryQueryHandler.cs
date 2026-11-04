using Content.Application.Common;
using Content.Application.Dashboard.DTOs;
using Dapper;
using SharedKernel.Application.CQRS;
using SharedKernel.Common;

namespace Content.Application.Dashboard.Queries;

/// <summary>
/// 获取仪表盘聚合统计数据处理器
/// </summary>
public sealed class GetDashboardSummaryQueryHandler : IQueryHandler<GetDashboardSummaryQuery, DashboardSummaryDto>
{
    private readonly IContentDbConnectionFactory _connectionFactory;
    private readonly ISystemStatsTracker _statsTracker;

    /// <summary>
    /// Initializes a new instance of the <see cref="GetDashboardSummaryQueryHandler"/> class.
    /// </summary>
    /// <param name="connectionFactory">数据库连接工厂</param>
    /// <param name="statsTracker">系统统计与访问量跟踪服务</param>
    public GetDashboardSummaryQueryHandler(
        IContentDbConnectionFactory connectionFactory,
        ISystemStatsTracker statsTracker)
    {
        _connectionFactory = connectionFactory;
        _statsTracker = statsTracker;
    }

    /// <inheritdoc />
    public async Task<DashboardSummaryDto> Handle(GetDashboardSummaryQuery request, CancellationToken cancellationToken)
    {
        using var connection = _connectionFactory.CreateConnection();

        // 1. 发起访问量异步查询（与数据库查询并发执行，支持 Redis 与单机内存降级）
        var visitsTask = _statsTracker.GetSystemVisitsAsync(cancellationToken);

        // 2. 使用 Dapper QueryMultipleAsync 单次网络往返 (Single Round-Trip) 批量执行全部 SQL
        // 彻底杜绝在同一物理连接上并发执行多条 SQL 引起的 NpgsqlOperationInProgressException 异常
        var batchSql = $"""
            SELECT COUNT(*) FROM articles WHERE is_del = false;
            SELECT COALESCE(SUM(views), 0) FROM articles WHERE is_del = false;
            SELECT COUNT(*) FROM series WHERE is_del = false;
            SELECT COALESCE(c.name, '未分类') AS category, COUNT(*) AS count
            FROM articles a
            LEFT JOIN categories c ON a.category_id = c.id
            WHERE a.is_del = false
            GROUP BY c.name
            ORDER BY count DESC;
            SELECT a.id, a.title, COALESCE(c.name, '未分类') AS category, a.views, a.author, a.created_at
            FROM articles a
            LEFT JOIN categories c ON a.category_id = c.id
            WHERE a.is_del = false
            ORDER BY a.created_at DESC, a.id DESC
            LIMIT 5;
            SELECT to_char(created_at AT TIME ZONE '{TimeZoneHelper.DbTimeZone}', 'MM-DD') AS date, COUNT(*) AS count
            FROM articles
            WHERE is_del = false AND created_at >= (CURRENT_DATE - INTERVAL '6 days')::timestamptz
            GROUP BY to_char(created_at AT TIME ZONE '{TimeZoneHelper.DbTimeZone}', 'MM-DD')
            ORDER BY date ASC;
            """;

        using var multi = await connection.QueryMultipleAsync(
            new CommandDefinition(batchSql, cancellationToken: cancellationToken));

        var totalArticles = await multi.ReadFirstOrDefaultAsync<int>();
        var totalViews = await multi.ReadFirstOrDefaultAsync<long>();
        var totalSeries = await multi.ReadFirstOrDefaultAsync<int>();
        var categories = (await multi.ReadAsync<CategoryStatDto>()).ToList();
        var recentArticles = (await multi.ReadAsync<RecentArticleDto>()).ToList();
        var trendRows = (await multi.ReadAsync<TrendRow>()).ToList();

        var totalVisits = await visitsTask;

        // 整理近 7 天完整日期数据（补齐没有发文的日期）
        var trendMap = trendRows.ToDictionary(r => r.Date, r => (int)r.Count);
        var trendList = new List<TrendItemDto>();
        var nowLocal = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, TimeZoneHelper.AppTimeZone);

        for (var i = 6; i >= 0; i--)
        {
            var d = nowLocal.Date.AddDays(-i).ToString("MM-dd");
            trendList.Add(new TrendItemDto
            {
                Date = d,
                ArticleCount = trendMap.GetValueOrDefault(d, 0)
            });
        }

        return new DashboardSummaryDto
        {
            TotalArticles = totalArticles,
            TotalVisits = totalVisits,
            TotalViews = totalViews,
            TotalSeries = totalSeries,
            CategoryDistribution = categories,
            RecentArticles = recentArticles,
            TrendData = trendList
        };
    }

    private sealed record TrendRow
    {
        public string Date { get; init; } = string.Empty;
        public long Count { get; init; }
    }
}
