using ApiHost.DTOs;
using Content.Application.Common;
using Identity.Application.Common.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using SharedKernel.Api;
using SharedKernel.Application;

namespace ApiHost.Controllers;

/// <summary>
/// 系统公开运行环境与特性能力探测控制器
/// </summary>
[AllowAnonymous]
[Route("api/system")]
public sealed class SystemController : BaseApiController
{
    private readonly IGitHubOAuthService _gitHubOAuthService;
    private readonly IOptions<AISettings> _aiOptions;
    private readonly IOptions<StorageSettings> _storageOptions;
    private readonly IConfiguration _configuration;

    /// <summary>
    /// Initializes a new instance of the <see cref="SystemController"/> class.
    /// </summary>
    /// <param name="gitHubOAuthService">GitHub 开放平台通信服务</param>
    /// <param name="aiOptions">AI 配置选项</param>
    /// <param name="storageOptions">存储模式配置选项</param>
    /// <param name="configuration">全局配置</param>
    public SystemController(
        IGitHubOAuthService gitHubOAuthService,
        IOptions<AISettings> aiOptions,
        IOptions<StorageSettings> storageOptions,
        IConfiguration configuration)
    {
        _gitHubOAuthService = gitHubOAuthService;
        _aiOptions = aiOptions;
        _storageOptions = storageOptions;
        _configuration = configuration;
    }

    /// <summary>
    /// 获取当前系统公开特性与运行配置（用于管理端与展示端自适应动态显隐入口）
    /// </summary>
    /// <returns>系统开启的能力清单与站点元数据</returns>
    [HttpGet("public-config")]
    [ProducesResponseType(typeof(ApiResult<PublicSystemConfigDto>), StatusCodes.Status200OK)]
    public IActionResult GetPublicConfig()
    {
        var redisEnabled = _configuration.GetValue<bool>("Redis:Enabled", false);
        var aiConfigured = _aiOptions.Value.IsConfigured;

        var dto = new PublicSystemConfigDto
        {
            Features = new SystemFeaturesDto
            {
                GithubLogin = _gitHubOAuthService.IsEnabled,
                AiAssistant = aiConfigured,
                AutoGenerateArticle = _aiOptions.Value.AutoGenerateEnabled && aiConfigured,
                StorageProvider = _storageOptions.Value.Provider,
                RedisEnabled = redisEnabled,
            },
            Site = new SystemInfoDto
            {
                SiteName = _configuration["Site:Name"] ?? "Chakra Blog System",
                Version = "1.0.0",
            },
        };

        return Success(dto);
    }
}
