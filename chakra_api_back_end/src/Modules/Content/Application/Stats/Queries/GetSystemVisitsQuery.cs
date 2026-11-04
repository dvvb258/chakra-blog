using SharedKernel.Application.CQRS;

namespace Content.Application.Stats.Queries;

/// <summary>
/// 获取系统总访问量查询
/// </summary>
public record GetSystemVisitsQuery : IQuery<long>;
