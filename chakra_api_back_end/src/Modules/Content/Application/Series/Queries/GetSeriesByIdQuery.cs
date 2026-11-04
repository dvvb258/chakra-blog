using Content.Application.Series.DTOs;
using SharedKernel.Application.CQRS;

namespace Content.Application.Series.Queries;

/// <summary>
/// 根据ID获取系列查询
/// </summary>
public record GetSeriesByIdQuery(long Id) : IQuery<SeriesDetailDto?>;
