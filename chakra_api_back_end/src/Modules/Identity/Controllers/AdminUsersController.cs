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
/// 管理端系统用户与角色权限控制器（需系统用户管理相关权限）
/// </summary>
[Authorize]
[Route("api/admin/users")]
public class AdminUsersController : BaseApiController
{
    private readonly IMediator _mediator;

    public AdminUsersController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// 分页查询系统用户列表
    /// </summary>
    /// <param name="query">过滤与分页参数</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>用户列表</returns>
    [HttpGet]
    [RequirePermission(Permissions.UserView)]
    [ProducesResponseType(typeof(ApiResult<PagedResult<UserListItemDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetUsersAsync([FromQuery] GetUsersQuery query, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(query, cancellationToken);
        return Success(result);
    }

    /// <summary>
    /// 修改指定用户的角色（如将 Editor 升级为 Admin）
    /// </summary>
    /// <param name="id">用户ID</param>
    /// <param name="request">角色变更参数</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>操作状态</returns>
    [HttpPut("{id:long}/role")]
    [RequirePermission(Permissions.UserRoleUpdate)]
    [ProducesResponseType(typeof(ApiResult<string>), StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateRoleAsync(long id, [FromBody] UpdateUserRoleRequest request, CancellationToken cancellationToken)
    {
        var command = new UpdateUserRoleCommand(id, request.Role);
        await _mediator.Send(command, cancellationToken);
        return Success("用户角色已成功更新");
    }

    /// <summary>
    /// 获取系统可用角色字典
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>角色列表</returns>
    [HttpGet("roles")]
    [RequirePermission(Permissions.UserView)]
    [ProducesResponseType(typeof(ApiResult<List<RoleDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetRolesAsync(CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetRolesQuery(), cancellationToken);
        return Success(result);
    }
}
