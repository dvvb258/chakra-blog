using SharedKernel.Application.Attributes;
using SharedKernel.Application.CQRS;
using SharedKernel.Constants;

namespace Content.Application.Articles.Commands;

/// <summary>
/// 切换文章置顶状态命令（限管理员）
/// </summary>
/// <param name="Id">文章ID</param>
/// <param name="IsPinned">是否置顶</param>
[AuditLog(AuditModules.Article, "切换文章置顶状态")]
public sealed record ToggleArticlePinCommand(long Id, bool IsPinned) : ICommand;
