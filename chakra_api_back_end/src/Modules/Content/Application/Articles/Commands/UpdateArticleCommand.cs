using Content.Application.Articles.DTOs;
using SharedKernel.Application.Attributes;
using SharedKernel.Application.CQRS;
using SharedKernel.Constants;

namespace Content.Application.Articles.Commands;

/// <summary>
/// 更新文章命令
/// </summary>
/// <param name="Id">文章ID</param>
/// <param name="Request">更新文章参数</param>
/// <param name="CurrentUserId">当前操作人用户ID</param>
[AuditLog(AuditModules.Article, "修改文章")]
public sealed record UpdateArticleCommand(
    long Id,
    UpdateArticleRequest Request,
    long CurrentUserId = 0) : ICommand;
