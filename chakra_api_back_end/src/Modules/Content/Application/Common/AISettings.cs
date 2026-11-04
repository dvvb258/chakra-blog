using SharedKernel.Common;

namespace Content.Application.Common;

/// <summary>
/// AI 服务配置（基于 OpenAI 兼容协议，支持 Cloudflare Workers AI / DeepSeek / Kimi 等）
/// </summary>
public class AISettings
{
    /// <summary>
    /// 配置节名称
    /// </summary>
    public const string SectionName = "AISettings";

    /// <summary>
    /// API 密钥
    /// </summary>
    public string Key { get; set; } = string.Empty;

    public bool IsConfigured => !ConfigurationSecrets.IsPlaceholder(Key)
        && !string.IsNullOrWhiteSpace(ModelName)
        && Uri.TryCreate(Endpoint, UriKind.Absolute, out var endpoint)
        && (endpoint.Scheme == Uri.UriSchemeHttps || endpoint.IsLoopback);

    /// <summary>
    /// API 端点（OpenAI 兼容格式）
    /// </summary>
    public string Endpoint { get; set; } = string.Empty;

    /// <summary>
    /// 模型名称
    /// </summary>
    public string ModelName { get; set; } = string.Empty;

    /// <summary>
    /// 是否开启后台 AI 定时自动撰写文章任务（默认 false 关闭）
    /// </summary>
    public bool AutoGenerateEnabled { get; set; } = false;

    /// <summary>
    /// RSS 技术订阅源列表（支持通过配置文件覆盖扩展）
    /// </summary>
    public List<string> RssSources { get; set; } =
    [
        "https://devblogs.microsoft.com/dotnet/feed/",
        "https://devblogs.microsoft.com/aspnet/feed/",
        "https://devblogs.microsoft.com/agent-framework/feed/",
    ];
}
