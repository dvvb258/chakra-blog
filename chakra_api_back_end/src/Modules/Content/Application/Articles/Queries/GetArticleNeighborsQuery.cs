using Content.Application.Articles.DTOs;
using SharedKernel.Application.CQRS;

namespace Content.Application.Articles.Queries;

/// <summary>
/// 获取文章的相邻上下篇查询
/// </summary>
/// <param name="Id">文章ID</param>
public sealed record GetArticleNeighborsQuery(long Id) : IQuery<ArticleNeighborsDto?>;
