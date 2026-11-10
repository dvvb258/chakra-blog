using SharedKernel.Application.Attributes;
using SharedKernel.Application.CQRS;

namespace Content.Application.Stats.Commands;

/// <summary>
/// 文章点赞数 +1 命令
/// </summary>
/// <param name="ArticleId">文章ID</param>
/// <param name="ClientIp">客户端IP地址</param>
/// <param name="VisitorId">前端访客唯一标识</param>
[SkipAuditLog]
public record IncrementArticleLikeCommand(long ArticleId, string? ClientIp = null, string? VisitorId = null) : ICommand;
