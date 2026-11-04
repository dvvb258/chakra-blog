using Content.Application.Articles.Commands;
using Content.Application.Articles.DTOs;
using Content.Application.Articles.Queries;
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
/// 管理端文章控制器（面向博主管理控制台，强制 JWT 鉴权，支持细粒度权限控制，无 HTTP 缓存）
/// </summary>
[Authorize]
[Route("api/admin/articles")]
public class AdminArticlesController : BaseApiController
{
    private readonly IMediator _mediator;
    private readonly ICurrentUser _currentUser;

    /// <summary>
    /// Initializes a new instance of the <see cref="AdminArticlesController"/> class.
    /// </summary>
    /// <param name="mediator">MediatR 调度器</param>
    /// <param name="currentUser">当前登录用户解析器</param>
    public AdminArticlesController(IMediator mediator, ICurrentUser currentUser)
    {
        _mediator = mediator;
        _currentUser = currentUser;
    }

    /// <summary>
    /// 获取管理端文章分页列表
    /// </summary>
    /// <param name="query">管理端过滤与分页参数</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>分页文章列表</returns>
    [HttpGet]
    [RequirePermission(Permissions.ArticleView)]
    [ProducesResponseType(typeof(ApiResult<PagedResult<ArticleListItemDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetListAsync([FromQuery] GetAdminArticlesQuery query, CancellationToken cancellationToken)
    {
        // 权限隔离：若当前用户不是超级管理员（即 Editor 内容编辑者），强制仅可查看自己创建的文章
        if (!_currentUser.IsInRole(RoleNames.Admin))
        {
            query.CreatedByUserId = _currentUser.UserId;
        }

        var result = await _mediator.Send(query, cancellationToken);
        return Success(result);
    }

    /// <summary>
    /// 获取管理端单篇文章详情（实时无缓存，含 Markdown 完整正文）
    /// </summary>
    /// <param name="id">文章 ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>文章详情</returns>
    [HttpGet("{id:long}")]
    [RequirePermission(Permissions.ArticleView)]
    [ProducesResponseType(typeof(ApiResult<ArticleDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetByIdAsync(long id, CancellationToken cancellationToken)
    {
        var article = await _mediator.Send(new GetAdminArticleByIdQuery(id), cancellationToken);
        if (article == null)
        {
            throw new NotFoundException("Article", id);
        }

        // 非管理员只可查看自己名下的文章详情（防横向越权 IDOR）
        if (!_currentUser.IsInRole(RoleNames.Admin) && article.CreatedByUserId != _currentUser.UserId)
        {
            throw new BusinessException("权限不足：您无权查看他人创建的文章", 403);
        }

        return Success(article);
    }

    /// <summary>
    /// 创建新文章
    /// </summary>
    /// <param name="request">创建文章参数</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>新建文章 ID</returns>
    [HttpPost]
    [RequirePermission(Permissions.ArticleCreate)]
    [ProducesResponseType(typeof(ApiResult<long>), StatusCodes.Status200OK)]
    public async Task<IActionResult> CreateAsync([FromBody] CreateArticleRequest request, CancellationToken cancellationToken)
    {
        var command = new CreateArticleCommand(
            request,
            _currentUser.UserId,
            _currentUser.Username,
            _currentUser.IsInRole(RoleNames.Admin));

        var id = await _mediator.Send(command, cancellationToken);
        return Success(id, "文章已成功保存");
    }

    /// <summary>
    /// 更新指定文章
    /// </summary>
    /// <param name="id">文章 ID</param>
    /// <param name="request">更新内容参数</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>操作状态</returns>
    [HttpPut("{id:long}")]
    [RequirePermission(Permissions.ArticleEdit)]
    [ProducesResponseType(typeof(ApiResult<string>), StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateAsync(long id, [FromBody] UpdateArticleRequest request, CancellationToken cancellationToken)
    {
        var command = new UpdateArticleCommand(
            id,
            request,
            _currentUser.UserId);

        await _mediator.Send(command, cancellationToken);
        return Success("文章更新成功");
    }

    /// <summary>
    /// 软删除指定文章
    /// </summary>
    /// <param name="id">文章 ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>操作状态</returns>
    [HttpDelete("{id:long}")]
    [RequirePermission(Permissions.ArticleDelete)]
    [ProducesResponseType(typeof(ApiResult<string>), StatusCodes.Status200OK)]
    public async Task<IActionResult> DeleteAsync(long id, CancellationToken cancellationToken)
    {
        var command = new DeleteArticleCommand(
            id,
            _currentUser.UserId,
            _currentUser.IsInRole(RoleNames.Admin));

        await _mediator.Send(command, cancellationToken);
        return Success("文章删除成功");
    }

    /// <summary>
    /// 提交文章审核（编辑者或管理员）
    /// </summary>
    /// <param name="id">文章 ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>操作状态</returns>
    [HttpPut("{id:long}/submit-review")]
    [RequirePermission(Permissions.ArticleSubmit, Permissions.ArticleEdit)]
    [ProducesResponseType(typeof(ApiResult<string>), StatusCodes.Status200OK)]
    public async Task<IActionResult> SubmitReviewAsync(long id, CancellationToken cancellationToken)
    {
        var command = new SubmitArticleReviewCommand(
            id,
            _currentUser.UserId);

        await _mediator.Send(command, cancellationToken);
        return Success("已成功提交管理员审核，请耐心等待审核发布");
    }

    /// <summary>
    /// 审核通过并发布文章
    /// </summary>
    /// <param name="id">文章 ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>操作状态</returns>
    [RequirePermission(Permissions.ArticleApprove)]
    [HttpPut("{id:long}/approve")]
    [ProducesResponseType(typeof(ApiResult<string>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ApproveAsync(long id, CancellationToken cancellationToken)
    {
        var command = new ApproveArticleCommand(id);
        await _mediator.Send(command, cancellationToken);
        return Success("文章审核通过并已正式发布上线");
    }

    /// <summary>
    /// 驳回文章审核并退回草稿
    /// </summary>
    /// <param name="id">文章 ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>操作状态</returns>
    [RequirePermission(Permissions.ArticleApprove)]
    [HttpPut("{id:long}/reject")]
    [ProducesResponseType(typeof(ApiResult<string>), StatusCodes.Status200OK)]
    public async Task<IActionResult> RejectAsync(long id, CancellationToken cancellationToken)
    {
        var command = new RejectArticleCommand(id);
        await _mediator.Send(command, cancellationToken);
        return Success("文章已被驳回并退回草稿");
    }
}
