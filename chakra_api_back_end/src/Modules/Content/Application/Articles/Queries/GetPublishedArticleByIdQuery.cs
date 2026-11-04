using Content.Application.Articles.DTOs;
using SharedKernel.Application.CQRS;

namespace Content.Application.Articles.Queries;

/// <summary>
/// 获取已发布文章详情查询
/// </summary>
/// <param name="Id">文章ID</param>
public sealed record GetPublishedArticleByIdQuery(long Id) : IQuery<ArticleDto?>;
