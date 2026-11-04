using Content.Application.Dashboard.DTOs;
using Content.Application.Dashboard.Queries;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SharedKernel.Api;
using SharedKernel.Application;
using SharedKernel.Application.Attributes;
using SharedKernel.Constants;

namespace Content.Controllers;

/// <summary>
/// 管理端仪表盘聚合数据控制器（需仪表盘查看权限）
/// </summary>
[RequirePermission(Permissions.DashboardView)]
[Route("api/admin/dashboard")]
public class AdminDashboardController : BaseApiController
{
    private readonly IMediator _mediator;

    public AdminDashboardController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// 获取仪表盘核心运营统计汇总数据（总访问量、总文章、总阅读量、总专栏、分类分布、最新文章、7天活跃趋势）
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>仪表盘聚合统计</returns>
    [HttpGet("summary")]
    [ProducesResponseType(typeof(ApiResult<DashboardSummaryDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSummaryAsync(CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetDashboardSummaryQuery(), cancellationToken);
        return Success(result);
    }
}
