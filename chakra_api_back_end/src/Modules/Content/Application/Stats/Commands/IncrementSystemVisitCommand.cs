using SharedKernel.Application.Attributes;
using SharedKernel.Application.CQRS;

namespace Content.Application.Stats.Commands;

/// <summary>
/// 系统访问量 +1 命令
/// </summary>
[SkipAuditLog]
public record IncrementSystemVisitCommand : ICommand;
