using Identity.Application.Users.Commands;
using Identity.Application.Users.DTOs;
using Identity.Application.Users.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SharedKernel.Api;
using SharedKernel.Application;
using SharedKernel.Application.Attributes;
using SharedKernel.Constants;

namespace Identity.Controllers;

/// <summary>
/// 管理端菜单与权限资源控制器（需系统菜单管理相关权限）
/// </summary>
[Authorize]
[Route("api/admin/menus")]
public class AdminMenusController : BaseApiController
{
    private readonly IMediator _mediator;

    /// <summary>
    /// Initializes a new instance of the <see cref="AdminMenusController"/> class.
    /// </summary>
    /// <param name="mediator">MediatR 调度器</param>
    public AdminMenusController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// 获取全量系统菜单与权限树列表（供角色权限树配置使用）
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>全量菜单列表</returns>
    [HttpGet]
    [RequirePermission(Permissions.MenuView)]
    [ProducesResponseType(typeof(ApiResult<List<MenuDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMenusAsync(CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetMenusQuery(), cancellationToken);
        return Success(result);
    }

    /// <summary>
    /// 获取后端权威注册的全量系统权限元数据字典（按模块分组，供前端权限配置与下拉联想使用）
    /// </summary>
    /// <returns>系统标准权限元数据集合</returns>
    [HttpGet("permissions-metadata")]
    [RequirePermission(Permissions.MenuView)]
    [ProducesResponseType(typeof(ApiResult<IReadOnlyList<PermissionMetadata>>), StatusCodes.Status200OK)]
    public IActionResult GetPermissionsMetadata()
    {
        return Success(Permissions.All);
    }

    /// <summary>
    /// 创建系统菜单项
    /// </summary>
    /// <param name="request">创建参数</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>创建后的菜单信息</returns>
    [HttpPost]
    [RequirePermission(Permissions.MenuEdit)]
    [ProducesResponseType(typeof(ApiResult<MenuDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> CreateMenuAsync([FromBody] CreateMenuRequest request, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new CreateMenuCommand(request), cancellationToken);
        return Success(result, "创建菜单成功");
    }

    /// <summary>
    /// 更新系统菜单项
    /// </summary>
    /// <param name="id">菜单ID</param>
    /// <param name="request">更新参数</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>更新后的菜单信息</returns>
    [HttpPut("{id:long}")]
    [RequirePermission(Permissions.MenuEdit)]
    [ProducesResponseType(typeof(ApiResult<MenuDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateMenuAsync([FromRoute] long id, [FromBody] UpdateMenuRequest request, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new UpdateMenuCommand(id, request), cancellationToken);
        return Success(result, "更新菜单成功");
    }

    /// <summary>
    /// 删除系统菜单项
    /// </summary>
    /// <param name="id">菜单ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>操作结果</returns>
    [HttpDelete("{id:long}")]
    [RequirePermission(Permissions.MenuEdit)]
    [ProducesResponseType(typeof(ApiResult<string>), StatusCodes.Status200OK)]
    public async Task<IActionResult> DeleteMenuAsync([FromRoute] long id, CancellationToken cancellationToken)
    {
        await _mediator.Send(new DeleteMenuCommand(id), cancellationToken);
        return Success("菜单已成功删除");
    }
}
