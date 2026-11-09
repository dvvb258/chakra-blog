using System.Text.Json;
using Content.Application.Settings.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SharedKernel.Api;
using SharedKernel.Application;

namespace Content.Controllers;

/// <summary>
/// 站点设置控制器（公开只读，供展示端运行期读取）
/// </summary>
[AllowAnonymous]
[Route("api/settings")]
public class SiteSettingsController : BaseApiController
{
    private readonly IMediator _mediator;

    public SiteSettingsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// 获取站点设置覆盖项（无配置时返回空对象）
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>配置覆盖项 JSON</returns>
    [HttpGet("site")]
    [ProducesResponseType(typeof(ApiResult<JsonElement>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAsync(CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetSiteSettingsQuery(), cancellationToken);
        return Success(result);
    }
}
