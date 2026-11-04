using SharedKernel.Application.CQRS;

namespace Content.Application.Articles.Queries;

/// <summary>
/// 获取全量文章热门标签列表查询
/// </summary>
/// <param name="Limit">最大返回数量（默认50）</param>
public sealed record GetTagsQuery(int Limit = 50) : IQuery<List<string>>;
