using Logging.Application.Logs.DTOs;
using Logging.Application.Logs.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SharedKernel.Api;
using SharedKernel.Application;
using SharedKernel.Application.Attributes;
using SharedKernel.Application.Exceptions;
using SharedKernel.Constants;

namespace Logging.Controllers;

/// <summary>
/// 管理端系统日志控制器（需日志查看相关权限，操作审计日志 + 系统未处理异常日志）
/// </summary>
[Authorize]
[Route("api/admin/logs")]
public class AdminLogsController : BaseApiController
{
    private readonly IMediator _mediator;

    public AdminLogsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// 分页查询操作审计日志
    /// </summary>
    /// <param name="query">过滤与分页参数</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>审计日志列表</returns>
    [HttpGet("audit")]
    [RequirePermission(Permissions.LogAuditView)]
    [ProducesResponseType(typeof(ApiResult<PagedResult<AuditLogDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAuditLogsAsync([FromQuery] GetAuditLogsQuery query, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(query, cancellationToken);
        return Success(result);
    }

    /// <summary>
    /// 分页查询系统异常日志
    /// </summary>
    /// <param name="query">过滤与分页参数</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>异常日志列表</returns>
    [HttpGet("exceptions")]
    [RequirePermission(Permissions.LogExceptionView)]
    [ProducesResponseType(typeof(ApiResult<PagedResult<ExceptionLogDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetExceptionLogsAsync([FromQuery] GetExceptionLogsQuery query, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(query, cancellationToken);
        return Success(result);
    }

    /// <summary>
    /// 获取指定系统异常日志详情（含完整堆栈追踪）
    /// </summary>
    /// <param name="id">日志ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>异常详情</returns>
    [HttpGet("exceptions/{id:long}")]
    [RequirePermission(Permissions.LogExceptionView)]
    [ProducesResponseType(typeof(ApiResult<ExceptionLogDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetExceptionDetailAsync(long id, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetExceptionLogByIdQuery(id), cancellationToken);
        if (result == null)
        {
            throw new NotFoundException("ExceptionLog", id);
        }

        return Success(result);
    }
}
