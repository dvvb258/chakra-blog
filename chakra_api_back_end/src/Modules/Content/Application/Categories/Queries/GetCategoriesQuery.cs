using Content.Application.Categories.DTOs;
using SharedKernel.Application.CQRS;

namespace Content.Application.Categories.Queries;

/// <summary>
/// 获取全量可用分类列表（含文章计数）查询
/// </summary>
public sealed record GetCategoriesQuery : IQuery<List<CategoryDto>>;
