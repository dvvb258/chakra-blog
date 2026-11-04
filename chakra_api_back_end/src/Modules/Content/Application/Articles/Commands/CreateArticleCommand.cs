using Content.Application.Articles.DTOs;
using SharedKernel.Application.Attributes;
using SharedKernel.Application.CQRS;
using SharedKernel.Constants;

namespace Content.Application.Articles.Commands;

/// <summary>
/// 创建文章命令
/// </summary>
/// <param name="Request">创建文章参数</param>
/// <param name="CurrentUserId">当前登录人用户ID</param>
/// <param name="CurrentUserNickname">当前登录人昵称（用于默认作者）</param>
/// <param name="IsAdmin">是否为系统超级管理员</param>
[AuditLog(AuditModules.Article, "创建文章")]
public sealed record CreateArticleCommand(
    CreateArticleRequest Request,
    long CurrentUserId,
    string? CurrentUserNickname = null,
    bool IsAdmin = false) : ICommand<long>;
