using SharedKernel.Application.CQRS;

namespace Content.Application.Stats.Commands;

/// <summary>
/// 文章点赞数 +1 命令
/// </summary>
/// <param name="ArticleId">文章ID</param>
public record IncrementArticleLikeCommand(long ArticleId) : ICommand;
