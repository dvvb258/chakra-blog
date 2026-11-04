using SharedKernel.Application.Attributes;
using SharedKernel.Application.CQRS;
using SharedKernel.Constants;

namespace Content.Application.Articles.Commands;

/// <summary>
/// 审核通过并发布文章命令（限超级管理员）
/// </summary>
/// <param name="Id">文章ID</param>
[AuditLog(AuditModules.Article, "审核通过文章")]
public sealed record ApproveArticleCommand(long Id) : ICommand;
