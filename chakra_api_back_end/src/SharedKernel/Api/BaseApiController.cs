using Microsoft.AspNetCore.Mvc;
using SharedKernel.Application;

namespace SharedKernel.Api;

/// <summary>
/// API控制器基类
/// </summary>
[ApiController]
[Route("api/[controller]")]
public abstract class BaseApiController : ControllerBase
{
    /// <summary>
    /// 返回成功结果
    /// </summary>
    /// <typeparam name="T">数据类型</typeparam>
    /// <param name="data">数据</param>
    /// <param name="message">消息</param>
    /// <returns>结果</returns>
    protected IActionResult Success<T>(T data, string message = "操作成功")
    {
        var traceId = HttpContext.Items["TraceId"] as string
                      ?? HttpContext.TraceIdentifier;
        return Ok(ApiResult<T>.Success(data, message, traceId));
    }
}
