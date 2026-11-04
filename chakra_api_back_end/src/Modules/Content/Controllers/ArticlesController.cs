using Content.Application.Articles.Queries;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using SharedKernel.Api;
using SharedKernel.Application.Exceptions;

namespace Content.Controllers;

/// <summary>
/// 公开端文章控制器（面向读者与展示端，带缓存）
/// </summary>
[Route("api/articles")]
public class ArticlesController : BaseApiController
{
    private readonly IMediator _mediator;

    /// <summary>
    /// Initializes a new instance of the <see cref="ArticlesController"/> class.
    /// </summary>
    /// <param name="mediator">MediatR 调度器</param>
    public ArticlesController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// 根据 ID 获取公开文章详情
    /// </summary>
    /// <param name="id">文章 ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>文章详情</returns>
    [HttpGet("{id:long}")]
    [ResponseCache(Duration = 60, Location = ResponseCacheLocation.Any)]
    public async Task<IActionResult> GetByIdAsync(long id, CancellationToken cancellationToken)
    {
        var article = await _mediator.Send(new GetPublishedArticleByIdQuery(id), cancellationToken);
        if (article == null)
        {
            throw new NotFoundException("Article", id);
        }

        return Success(article);
    }

    /// <summary>
    /// 根据 ID 获取公开文章的相邻上下篇
    /// </summary>
    /// <param name="id">文章 ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>相邻文章信息</returns>
    [HttpGet("{id:long}/neighbors")]
    [ResponseCache(Duration = 60, Location = ResponseCacheLocation.Any)]
    public async Task<IActionResult> GetNeighborsAsync(long id, CancellationToken cancellationToken)
    {
        var neighbors = await _mediator.Send(new GetArticleNeighborsQuery(id), cancellationToken);
        if (neighbors == null)
        {
            throw new NotFoundException("Article", id);
        }

        return Success(neighbors);
    }

    /// <summary>
    /// 获取公开前台文章列表
    /// </summary>
    /// <param name="query">查询参数</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>分页文章列表</returns>
    [HttpGet]
    [ResponseCache(Duration = 60, Location = ResponseCacheLocation.Any)]
    public async Task<IActionResult> GetListAsync([FromQuery] GetPublishedArticlesQuery query, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(query, cancellationToken);
        return Success(result);
    }
}
