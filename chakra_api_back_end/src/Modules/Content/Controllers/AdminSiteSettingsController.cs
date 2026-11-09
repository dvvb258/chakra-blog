using System.Text.Json;
using Content.Application.Settings.Commands;
using Content.Application.Settings.DTOs;
using Content.Application.Settings.Queries;
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
/// 管理端站点设置控制器（需站点设置权限）
/// </summary>
[Authorize]
[Route("api/admin/settings")]
public class AdminSiteSettingsController : BaseApiController
{
    private readonly IMediator _mediator;

    public AdminSiteSettingsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// 获取当前站点设置覆盖项（管理端编辑用）
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>配置覆盖项 JSON</returns>
    [HttpGet("site")]
    [RequirePermission(Permissions.SettingsManage)]
    [ProducesResponseType(typeof(ApiResult<JsonElement>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAsync(CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetSiteSettingsQuery(), cancellationToken);
        return Success(result);
    }

    /// <summary>
    /// 保存站点设置覆盖项（整体覆盖，未提交的字段回落代码默认值）
    /// </summary>
    /// <param name="request">配置覆盖项</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>操作状态</returns>
    [HttpPut("site")]
    [RequirePermission(Permissions.SettingsManage)]
    [ProducesResponseType(typeof(ApiResult<string>), StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateAsync([FromBody] UpdateSiteSettingsRequest request, CancellationToken cancellationToken)
    {
        // 未提交 config 时传 null 交给验证器拒绝，避免误清空现有配置（清空请显式提交 {}）
        var rawJson = request.Config?.GetRawText() ?? "null";
        await _mediator.Send(new UpdateSiteSettingsCommand(rawJson), cancellationToken);
        return Success("站点设置已保存");
    }
}
