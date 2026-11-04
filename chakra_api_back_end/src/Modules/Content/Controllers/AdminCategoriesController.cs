using Content.Application.Categories.Commands;
using Content.Application.Categories.DTOs;
using Content.Application.Categories.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SharedKernel.Api;
using SharedKernel.Application;
using SharedKernel.Application.Attributes;
using SharedKernel.Constants;

namespace Content.Controllers;

/// <summary>
/// 管理端文章分类控制器（需分类管理相关权限）
/// </summary>
[Authorize]
[Route("api/admin/categories")]
public class AdminCategoriesController : BaseApiController
{
    private readonly IMediator _mediator;

    public AdminCategoriesController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// 获取管理端全量文章分类列表（含文章计数）
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>分类列表</returns>
    [HttpGet]
    [RequirePermission(Permissions.CategoryView)]
    [ProducesResponseType(typeof(ApiResult<List<CategoryDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetListAsync(CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetCategoriesQuery(), cancellationToken);
        return Success(result);
    }

    /// <summary>
    /// 创建新文章分类
    /// </summary>
    /// <param name="request">创建参数</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>新建分类 ID</returns>
    [HttpPost]
    [RequirePermission(Permissions.CategoryCreate)]
    [ProducesResponseType(typeof(ApiResult<long>), StatusCodes.Status200OK)]
    public async Task<IActionResult> CreateAsync([FromBody] CreateCategoryRequest request, CancellationToken cancellationToken)
    {
        var id = await _mediator.Send(new CreateCategoryCommand(request), cancellationToken);
        return Success(id, "文章分类创建成功");
    }

    /// <summary>
    /// 更新指定文章分类
    /// </summary>
    /// <param name="id">分类 ID</param>
    /// <param name="request">更新参数</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>操作状态</returns>
    [HttpPut("{id:long}")]
    [RequirePermission(Permissions.CategoryEdit)]
    [ProducesResponseType(typeof(ApiResult<string>), StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateAsync(long id, [FromBody] UpdateCategoryRequest request, CancellationToken cancellationToken)
    {
        await _mediator.Send(new UpdateCategoryCommand(id, request), cancellationToken);
        return Success("文章分类更新成功");
    }

    /// <summary>
    /// 软删除指定文章分类
    /// </summary>
    /// <param name="id">分类 ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>操作状态</returns>
    [HttpDelete("{id:long}")]
    [RequirePermission(Permissions.CategoryDelete)]
    [ProducesResponseType(typeof(ApiResult<string>), StatusCodes.Status200OK)]
    public async Task<IActionResult> DeleteAsync(long id, CancellationToken cancellationToken)
    {
        await _mediator.Send(new DeleteCategoryCommand(id), cancellationToken);
        return Success("文章分类已成功删除");
    }
}
