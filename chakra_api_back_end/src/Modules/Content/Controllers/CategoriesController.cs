using Content.Application.Categories.DTOs;
using Content.Application.Categories.Queries;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SharedKernel.Api;
using SharedKernel.Application;

namespace Content.Controllers;

/// <summary>
/// 文章分类控制器（公开访问）
/// </summary>
[Route("api/categories")]
public class CategoriesController : BaseApiController
{
    private readonly IMediator _mediator;

    public CategoriesController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// 获取全量文章分类列表（含已发布文章计数，按排序号升序排列）
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>分类列表</returns>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResult<List<CategoryDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetListAsync(CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetCategoriesQuery(), cancellationToken);
        return Success(result);
    }
}
