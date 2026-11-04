using Content.Application.Stats.Commands;
using Content.Application.Stats.Queries;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using SharedKernel.Api;

namespace Content.Controllers;

/// <summary>
/// 统计控制器
/// </summary>
public class StatsController : BaseApiController
{
    private readonly IMediator _mediator;

    /// <summary>
    /// Initializes a new instance of the <see cref="StatsController"/> class.
    /// </summary>
    /// <param name="mediator">MediatR调度器</param>
    public StatsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// 文章阅读量 +1
    /// </summary>
    /// <param name="id">文章ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    [HttpPost("articles/{id:long}/view")]
    [EnableRateLimiting("stats-visit")]
    public async Task<IActionResult> IncrementArticleViewAsync(long id, CancellationToken cancellationToken)
    {
        await _mediator.Send(new IncrementArticleViewCommand(id), cancellationToken);
        return Success<object?>(null, "阅读量已更新");
    }

    /// <summary>
    /// 文章点赞数 +1
    /// </summary>
    /// <param name="id">文章ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    [HttpPost("articles/{id:long}/like")]
    [EnableRateLimiting("stats-visit")]
    public async Task<IActionResult> IncrementArticleLikeAsync(long id, CancellationToken cancellationToken)
    {
        await _mediator.Send(new IncrementArticleLikeCommand(id), cancellationToken);
        return Success<object?>(null, "点赞成功");
    }

    /// <summary>
    /// 系统访问量 +1（由前端页面跳转时调用）
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    [HttpPost("visit")]
    [EnableRateLimiting("stats-visit")]
    public async Task<IActionResult> IncrementSystemVisitAsync(CancellationToken cancellationToken)
    {
        await _mediator.Send(new IncrementSystemVisitCommand(), cancellationToken);
        return Success<object?>(null, "访问量已更新");
    }

    /// <summary>
    /// 获取系统总访问量
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>系统总访问量</returns>
    [HttpGet("visits")]
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public async Task<IActionResult> GetSystemVisitsAsync(CancellationToken cancellationToken)
    {
        var visits = await _mediator.Send(new GetSystemVisitsQuery(), cancellationToken);
        return Success(new { Visits = visits });
    }
}
