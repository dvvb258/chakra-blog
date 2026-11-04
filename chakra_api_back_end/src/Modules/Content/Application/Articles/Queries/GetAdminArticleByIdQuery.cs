using Content.Application.Articles.DTOs;
using SharedKernel.Application.CQRS;

namespace Content.Application.Articles.Queries;

/// <summary>
/// 管理端获取文章详情查询（实时无缓存，不触发前台阅读量累加）
/// </summary>
/// <param name="Id">文章ID</param>
public sealed record GetAdminArticleByIdQuery(long Id) : IQuery<ArticleDto?>;
