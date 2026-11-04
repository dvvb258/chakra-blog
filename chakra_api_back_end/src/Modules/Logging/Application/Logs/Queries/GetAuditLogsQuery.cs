using Logging.Application.Logs.DTOs;
using SharedKernel.Application;
using SharedKernel.Application.CQRS;

namespace Logging.Application.Logs.Queries;

/// <summary>
/// 查询操作审计日志分页列表
/// </summary>
public sealed record GetAuditLogsQuery : IQuery<PagedResult<AuditLogDto>>
{
    /// <summary>所属功能模块筛选</summary>
    public string? Module { get; init; }

    /// <summary>操作人用户名筛选</summary>
    public string? Username { get; init; }

    /// <summary>执行状态筛选（true: 成功, false: 失败）</summary>
    public bool? IsSuccess { get; init; }

    /// <summary>全链路追踪 Trace ID 筛选</summary>
    public string? TraceId { get; init; }

    /// <summary>开始时间（UTC）</summary>
    public DateTime? StartDate { get; init; }

    /// <summary>结束时间（UTC）</summary>
    public DateTime? EndDate { get; init; }

    private int _pageIndex = 1;
    private int _pageSize = 20;

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
        init => _pageSize = value < 1 ? 20 : (value > 100 ? 100 : value);
    }
}
