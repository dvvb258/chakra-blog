using SharedKernel.Application.Attributes;
using SharedKernel.Application.CQRS;
using SharedKernel.Constants;

namespace Content.Application.Categories.Commands;

/// <summary>
/// 软删除文章分类命令
/// </summary>
[AuditLog(AuditModules.Category, "删除文章分类")]
public sealed record DeleteCategoryCommand(long Id) : ICommand;
