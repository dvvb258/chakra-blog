using Content.Application.Series.Commands;
using Content.Application.Series.DTOs;
using Content.Application.Series.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SharedKernel.Api;
using SharedKernel.Application;
using SharedKernel.Application.Attributes;
using SharedKernel.Application.Exceptions;
using SharedKernel.Constants;

namespace Content.Controllers;

/// <summary>
/// 管理端专栏系列控制器（面向博主/管理员，需专栏管理相关权限）
/// </summary>
[Authorize]
[Route("api/admin/series")]
public class AdminSeriesController : BaseApiController
{
    private readonly IMediator _mediator;
    private readonly ICurrentUser _currentUser;

    public AdminSeriesController(IMediator mediator, ICurrentUser currentUser)
    {
        _mediator = mediator;
        _currentUser = currentUser;
    }

    /// <summary>
    /// 获取专栏系列分页列表
    /// </summary>
    /// <param name="query">查询参数</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>专栏分页列表</returns>
    [HttpGet]
    [RequirePermission(Permissions.SeriesView)]
    [ProducesResponseType(typeof(ApiResult<PagedResult<SeriesListItemDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetListAsync([FromQuery] GetSeriesListQuery query, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(query, cancellationToken);
        return Success(result);
    }

    /// <summary>
    /// 获取指定专栏系列详情（含归属文章列表）
    /// </summary>
    /// <param name="id">专栏ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>专栏详情</returns>
    [HttpGet("{id:long}")]
    [RequirePermission(Permissions.SeriesView)]
    [ProducesResponseType(typeof(ApiResult<SeriesDetailDto>), StatusCodes.Status200OK)]
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
    /// 创建新专栏系列
    /// </summary>
    /// <param name="request">创建参数</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>新创建的专栏ID</returns>
    [HttpPost]
    [RequirePermission(Permissions.SeriesCreate)]
    [ProducesResponseType(typeof(ApiResult<long>), StatusCodes.Status200OK)]
    public async Task<IActionResult> CreateAsync([FromBody] CreateSeriesRequest request, CancellationToken cancellationToken)
    {
        var command = new CreateSeriesCommand(request, _currentUser.Username);
        var id = await _mediator.Send(command, cancellationToken);
        return Success(id, "专栏创建成功");
    }

    /// <summary>
    /// 更新指定专栏系列
    /// </summary>
    /// <param name="id">专栏ID</param>
    /// <param name="request">更新参数</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>操作状态</returns>
    [HttpPut("{id:long}")]
    [RequirePermission(Permissions.SeriesEdit)]
    [ProducesResponseType(typeof(ApiResult<string>), StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateAsync(long id, [FromBody] UpdateSeriesRequest request, CancellationToken cancellationToken)
    {
        var command = new UpdateSeriesCommand(id, request);
        await _mediator.Send(command, cancellationToken);
        return Success("专栏更新成功");
    }

    /// <summary>
    /// 软删除指定专栏系列（将自动解除归属该专栏文章的绑定）
    /// </summary>
    /// <param name="id">专栏ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>操作状态</returns>
    [HttpDelete("{id:long}")]
    [RequirePermission(Permissions.SeriesDelete)]
    [ProducesResponseType(typeof(ApiResult<string>), StatusCodes.Status200OK)]
    public async Task<IActionResult> DeleteAsync(long id, CancellationToken cancellationToken)
    {
        var command = new DeleteSeriesCommand(id);
        await _mediator.Send(command, cancellationToken);
        return Success("专栏删除成功");
    }
}
