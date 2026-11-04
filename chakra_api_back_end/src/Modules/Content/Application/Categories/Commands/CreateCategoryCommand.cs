using Content.Application.Categories.DTOs;
using SharedKernel.Application.Attributes;
using SharedKernel.Application.CQRS;
using SharedKernel.Constants;

namespace Content.Application.Categories.Commands;

/// <summary>
/// 创建文章分类命令
/// </summary>
[AuditLog(AuditModules.Category, "创建文章分类")]
public sealed record CreateCategoryCommand(CreateCategoryRequest Request) : ICommand<long>;
