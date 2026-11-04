using Content.Application.Articles.Queries;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SharedKernel.Api;
using SharedKernel.Application;

namespace Content.Controllers;

/// <summary>
/// 文章标签控制器（公开访问）
/// </summary>
[Route("api/tags")]
public class TagsController : BaseApiController
{
    private readonly IMediator _mediator;

    public TagsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// 获取全站文章热门标签列表（按使用频次降序）
    /// </summary>
    /// <param name="limit">返回最大数量（默认50）</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>标签列表</returns>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResult<List<string>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetTagsAsync([FromQuery] int limit = 50, CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(new GetTagsQuery(limit), cancellationToken);
        return Success(result);
    }
}
