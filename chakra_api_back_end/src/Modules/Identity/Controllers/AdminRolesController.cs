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
/// 管理端角色与权限管理控制器（需角色管理相关权限）
/// </summary>
[Authorize]
[Route("api/admin/roles")]
public class AdminRolesController : BaseApiController
{
    private readonly IMediator _mediator;

    /// <summary>
    /// Initializes a new instance of the <see cref="AdminRolesController"/> class.
    /// </summary>
    /// <param name="mediator">MediatR 调度器</param>
    public AdminRolesController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// 获取全部角色列表（含已关联的菜单权限列表）
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>角色列表</returns>
    [HttpGet]
    [RequirePermission(Permissions.RoleView)]
    [ProducesResponseType(typeof(ApiResult<List<RoleDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetRolesAsync(CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetRolesQuery(), cancellationToken);
        return Success(result);
    }

    /// <summary>
    /// 创建新自定义角色
    /// </summary>
    /// <param name="request">创建角色参数</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>新建角色信息</returns>
    [HttpPost]
    [RequirePermission(Permissions.RoleCreate)]
    [ProducesResponseType(typeof(ApiResult<RoleDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> CreateRoleAsync([FromBody] CreateRoleRequest request, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new CreateRoleCommand(request), cancellationToken);
        return Success(result, "角色已成功创建");
    }

    /// <summary>
    /// 更新指定角色的基本信息与菜单权限配置
    /// </summary>
    /// <param name="id">角色ID</param>
    /// <param name="request">更新参数</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>更新后的角色信息</returns>
    [HttpPut("{id:long}")]
    [RequirePermission(Permissions.RoleEdit)]
    [ProducesResponseType(typeof(ApiResult<RoleDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateRoleAsync(long id, [FromBody] UpdateRoleRequest request, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new UpdateRoleCommand(id, request), cancellationToken);
        return Success(result, "角色权限已成功更新");
    }

    /// <summary>
    /// 删除指定角色（系统内置角色不可删除）
    /// </summary>
    /// <param name="id">角色ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>操作结果</returns>
    [HttpDelete("{id:long}")]
    [RequirePermission(Permissions.RoleDelete)]
    [ProducesResponseType(typeof(ApiResult<string>), StatusCodes.Status200OK)]
    public async Task<IActionResult> DeleteRoleAsync(long id, CancellationToken cancellationToken)
    {
        await _mediator.Send(new DeleteRoleCommand(id), cancellationToken);
        return Success("角色已成功删除");
    }
}
