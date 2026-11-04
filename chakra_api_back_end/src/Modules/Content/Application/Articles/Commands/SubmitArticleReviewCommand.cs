using SharedKernel.Application.Attributes;
using SharedKernel.Application.CQRS;
using SharedKernel.Constants;

namespace Content.Application.Articles.Commands;

/// <summary>
/// 提交文章审核命令（仅限文章作者本人）
/// </summary>
/// <param name="Id">文章ID</param>
/// <param name="CurrentUserId">当前操作人用户ID</param>
[AuditLog(AuditModules.Article, "提交文章审核")]
public sealed record SubmitArticleReviewCommand(
    long Id,
    long CurrentUserId = 0) : ICommand;
