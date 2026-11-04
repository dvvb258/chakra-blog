using SharedKernel.Application.Attributes;
using SharedKernel.Application.CQRS;
using SharedKernel.Constants;

namespace Content.Application.Articles.Commands;

/// <summary>
/// 软删除文章命令
/// </summary>
/// <param name="Id">文章ID</param>
/// <param name="CurrentUserId">当前操作人用户ID</param>
/// <param name="IsAdmin">是否为系统超级管理员</param>
[AuditLog(AuditModules.Article, "删除文章")]
public sealed record DeleteArticleCommand(
    long Id,
    long CurrentUserId = 0,
    bool IsAdmin = false) : ICommand;
