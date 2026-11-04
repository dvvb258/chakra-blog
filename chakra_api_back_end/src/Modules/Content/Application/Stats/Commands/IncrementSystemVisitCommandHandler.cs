using Content.Application.Common;
using SharedKernel.Application.CQRS;

namespace Content.Application.Stats.Commands;

/// <summary>
/// 系统访问量 +1 命令处理器
/// </summary>
public sealed class IncrementSystemVisitCommandHandler : ICommandHandler<IncrementSystemVisitCommand>
{
    private readonly ISystemStatsTracker _statsTracker;

    /// <summary>
    /// Initializes a new instance of the <see cref="IncrementSystemVisitCommandHandler"/> class.
    /// </summary>
    /// <param name="statsTracker">系统统计与访问量跟踪服务</param>
    public IncrementSystemVisitCommandHandler(ISystemStatsTracker statsTracker)
    {
        _statsTracker = statsTracker;
    }

    /// <inheritdoc />
    public async Task Handle(IncrementSystemVisitCommand request, CancellationToken cancellationToken)
    {
        await _statsTracker.IncrementSystemVisitsAsync(cancellationToken);
    }
}
