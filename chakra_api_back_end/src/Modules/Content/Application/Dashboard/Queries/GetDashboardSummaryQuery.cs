using Content.Application.Dashboard.DTOs;
using SharedKernel.Application.CQRS;

namespace Content.Application.Dashboard.Queries;

/// <summary>
/// 获取仪表盘聚合统计数据查询
/// </summary>
public sealed record GetDashboardSummaryQuery : IQuery<DashboardSummaryDto>;
