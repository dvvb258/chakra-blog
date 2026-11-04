using SharedKernel.Application.CQRS;

namespace Content.Application.Stats.Commands;

/// <summary>
/// 系统访问量 +1 命令
/// </summary>
public record IncrementSystemVisitCommand : ICommand;
