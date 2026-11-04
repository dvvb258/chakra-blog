namespace Identity.Application.Common;

/// <summary>
/// GitHub OAuth 身份认证配置项
/// </summary>
public sealed class GitHubSettings
{
    /// <summary>
    /// 配置文件对应的节点名称
    /// </summary>
    public const string SectionName = "Identity:GitHub";

    /// <summary>
    /// 是否开启 GitHub 第三方登录功能（默认 false 关闭）
    /// </summary>
    public bool Enabled { get; set; } = false;

    /// <summary>
    /// GitHub OAuth 客户端 ID
    /// </summary>
    public string ClientId { get; set; } = string.Empty;

    /// <summary>
    /// GitHub OAuth 客户端密钥
    /// </summary>
    public string ClientSecret { get; set; } = string.Empty;

    /// <summary>
    /// 前端回调重定向地址
    /// </summary>
    public string RedirectUri { get; set; } = string.Empty;
}
