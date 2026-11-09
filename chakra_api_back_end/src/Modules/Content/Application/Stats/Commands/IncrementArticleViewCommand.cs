using SharedKernel.Application.Attributes;
using SharedKernel.Application.CQRS;

namespace Content.Application.Stats.Commands;

/// <summary>
/// 文章阅读量 +1 命令
/// </summary>
[SkipAuditLog]
public record IncrementArticleViewCommand(long ArticleId) : ICommand;
