using SharedKernel.Application.Attributes;
using SharedKernel.Application.CQRS;
using SharedKernel.Constants;

namespace Content.Application.Articles.Commands;

/// <summary>
/// 驳回/退回文章审核命令（限超级管理员）
/// </summary>
/// <param name="Id">文章ID</param>
[AuditLog(AuditModules.Article, "驳回文章审核")]
public sealed record RejectArticleCommand(long Id) : ICommand;
