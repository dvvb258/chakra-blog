using Content.Application.Series.Queries;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using SharedKernel.Api;
using SharedKernel.Application.Exceptions;

namespace Content.Controllers;

/// <summary>
/// 系列控制器
/// </summary>
[Route("api/series")]
public class SeriesController : BaseApiController
{
    private readonly IMediator _mediator;

    /// <summary>
    /// Initializes a new instance of the <see cref="SeriesController"/> class.
    /// </summary>
    /// <param name="mediator">MediatR调度器</param>
    public SeriesController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// 根据ID获取系列
    /// </summary>
    /// <param name="id">系列ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>系列详情</returns>
    [HttpGet("{id:long}")]
    [ResponseCache(Duration = 3600, Location = ResponseCacheLocation.Any)]
    public async Task<IActionResult> GetByIdAsync(long id, CancellationToken cancellationToken)
    {
        var series = await _mediator.Send(new GetSeriesByIdQuery(id), cancellationToken);
        if (series == null)
        {
            throw new NotFoundException("Series", id);
        }

        return Success(series);
    }

    /// <summary>
    /// 获取系列列表
    /// </summary>
    /// <param name="query">查询参数</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>系列列表</returns>
    [HttpGet]
    [ResponseCache(Duration = 3600, Location = ResponseCacheLocation.Any)]
    public async Task<IActionResult> GetListAsync([FromQuery] GetSeriesListQuery query, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(query, cancellationToken);
        return Success(result);
    }
}
