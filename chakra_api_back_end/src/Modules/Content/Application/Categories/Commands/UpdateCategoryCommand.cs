using Content.Application.Categories.DTOs;
using SharedKernel.Application.Attributes;
using SharedKernel.Application.CQRS;
using SharedKernel.Constants;

namespace Content.Application.Categories.Commands;

/// <summary>
/// 更新文章分类命令
/// </summary>
[AuditLog(AuditModules.Category, "更新文章分类")]
public sealed record UpdateCategoryCommand(long Id, UpdateCategoryRequest Request) : ICommand;
