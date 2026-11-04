using Content.Application.Common;
using SharedKernel.Application.CQRS;

namespace Content.Application.Stats.Queries;

/// <summary>
/// 获取系统总访问量查询处理器
/// </summary>
public sealed class GetSystemVisitsQueryHandler : IQueryHandler<GetSystemVisitsQuery, long>
{
    private readonly ISystemStatsTracker _statsTracker;

    /// <summary>
    /// Initializes a new instance of the <see cref="GetSystemVisitsQueryHandler"/> class.
    /// </summary>
    /// <param name="statsTracker">系统统计与访问量跟踪服务</param>
    public GetSystemVisitsQueryHandler(ISystemStatsTracker statsTracker)
    {
        _statsTracker = statsTracker;
    }

    /// <inheritdoc />
    public async Task<long> Handle(GetSystemVisitsQuery request, CancellationToken cancellationToken)
    {
        return await _statsTracker.GetSystemVisitsAsync(cancellationToken);
    }
}
