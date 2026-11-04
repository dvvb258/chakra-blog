using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Identity.Application.Common;
using Identity.Application.Common.Interfaces;
using Identity.Application.Common.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SharedKernel.Application.Exceptions;
using SharedKernel.Common;

namespace Identity.Infrastructure.Services;

/// <summary>
/// GitHub OAuth 开放平台通讯服务实现
/// </summary>
public sealed class GitHubOAuthService : IGitHubOAuthService
{
    /// <summary>
    /// GitHub API 请求中的 User-Agent 标识（GitHub 强制要求，可在此统一修改）
    /// </summary>
    private static readonly ProductInfoHeaderValue UserAgentValue = new("blog-api-service", "1.0");
    private readonly HttpClient _httpClient;
    private readonly GitHubSettings _settings;
    private readonly ILogger<GitHubOAuthService> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="GitHubOAuthService"/> class.
    /// </summary>
    /// <param name="httpClient">HTTP 客户端</param>
    /// <param name="options">GitHub 配置项选项</param>
    /// <param name="logger">日志记录器</param>
    public GitHubOAuthService(
        HttpClient httpClient,
        IOptions<GitHubSettings> options,
        ILogger<GitHubOAuthService> logger)
    {
        _httpClient = httpClient;
        _settings = options.Value;
        _logger = logger;
    }

    /// <inheritdoc />
    public bool IsEnabled => _settings.Enabled &&
        !ConfigurationSecrets.IsPlaceholder(_settings.ClientId) &&
        !ConfigurationSecrets.IsPlaceholder(_settings.ClientSecret) &&
        Uri.TryCreate(_settings.RedirectUri, UriKind.Absolute, out var redirectUri) &&
        (redirectUri.Scheme == Uri.UriSchemeHttps || redirectUri.IsLoopback);

    /// <inheritdoc />
    public string GetAuthorizationUrl(string state)
    {
        if (!IsEnabled)
        {
            throw new BusinessException("系统尚未启用或未配置 GitHub 第三方登录功能", 400);
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(state);
        var encodedRedirectUri = Uri.EscapeDataString(_settings.RedirectUri);

        return $"https://github.com/login/oauth/authorize?client_id={_settings.ClientId}&redirect_uri={encodedRedirectUri}&scope=read:user,user:email&state={Uri.EscapeDataString(state.Trim())}";
    }

    /// <inheritdoc />
    public async Task<GitHubUserInfo> GetUserInfoByCodeAsync(string code, CancellationToken cancellationToken = default)
    {
        if (!IsEnabled)
        {
            throw new BusinessException("系统尚未启用或未配置 GitHub 第三方登录功能", 400);
        }

        // 1. 发起 POST 请求向 GitHub 换取 Access Token
        var tokenPayload = new Dictionary<string, string>
        {
            ["client_id"] = _settings.ClientId,
            ["client_secret"] = _settings.ClientSecret,
            ["code"] = code.Trim(),
        };

        if (!string.IsNullOrWhiteSpace(_settings.RedirectUri))
        {
            tokenPayload["redirect_uri"] = _settings.RedirectUri;
        }

        using var tokenRequest = new HttpRequestMessage(HttpMethod.Post, "https://github.com/login/oauth/access_token")
        {
            Content = new FormUrlEncodedContent(tokenPayload),
        };
        tokenRequest.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        tokenRequest.Headers.UserAgent.Add(UserAgentValue);

        HttpResponseMessage tokenResponse;
        try
        {
            tokenResponse = await _httpClient.SendAsync(tokenRequest, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "请求 GitHub access_token 发生网络异常");
            throw new BusinessException("连接 GitHub 认证服务器超时或网络异常，请重试", 502);
        }

        if (!tokenResponse.IsSuccessStatusCode)
        {
            var errContent = await tokenResponse.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogError("GitHub 换取令牌失败，状态码: {StatusCode}, 内容: {Content}", tokenResponse.StatusCode, errContent);
            throw new BusinessException("GitHub 身份凭据交换失败", 400);
        }

        var tokenData = await tokenResponse.Content.ReadFromJsonAsync<GitHubTokenResponse>(cancellationToken: cancellationToken);
        if (tokenData is null || !string.IsNullOrWhiteSpace(tokenData.Error) || string.IsNullOrWhiteSpace(tokenData.AccessToken))
        {
            var msg = tokenData?.ErrorDescription ?? tokenData?.Error ?? "无效的授权码或已过期";
            _logger.LogWarning("GitHub 换取令牌业务错误: {Message}", msg);
            throw new BusinessException($"GitHub 授权失效: {msg}", 400);
        }

        // 2. 根据获取到的 AccessToken 请求 GitHub 用户资料
        using var userRequest = new HttpRequestMessage(HttpMethod.Get, "https://api.github.com/user");
        userRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", tokenData.AccessToken);
        userRequest.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        userRequest.Headers.UserAgent.Add(UserAgentValue);

        var userResponse = await _httpClient.SendAsync(userRequest, cancellationToken);
        if (!userResponse.IsSuccessStatusCode)
        {
            _logger.LogError("读取 GitHub 用户资料失败，状态码: {StatusCode}", userResponse.StatusCode);
            throw new BusinessException("读取 GitHub 用户资料失败", 400);
        }

        var githubRawUser = await userResponse.Content.ReadFromJsonAsync<GitHubRawUserResponse>(cancellationToken: cancellationToken);
        if (githubRawUser is null || githubRawUser.Id <= 0)
        {
            throw new BusinessException("解析 GitHub 用户信息失败", 400);
        }

        // 3. 只接受 GitHub 明确标记为已验证的邮箱
        var primaryEmail = await TryGetVerifiedEmailAsync(tokenData.AccessToken, cancellationToken);

        return new GitHubUserInfo
        {
            Id = githubRawUser.Id.ToString(),
            Login = githubRawUser.Login,
            Name = githubRawUser.Name,
            AvatarUrl = githubRawUser.AvatarUrl,
            Email = primaryEmail,
            Bio = githubRawUser.Bio,
        };
    }

    private async Task<string?> TryGetVerifiedEmailAsync(string accessToken, CancellationToken cancellationToken)
    {
        try
        {
            using var emailsRequest = new HttpRequestMessage(HttpMethod.Get, "https://api.github.com/user/emails");
            emailsRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            emailsRequest.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            emailsRequest.Headers.UserAgent.Add(UserAgentValue);

            var emailsResponse = await _httpClient.SendAsync(emailsRequest, cancellationToken);
            if (!emailsResponse.IsSuccessStatusCode)
            {
                return null;
            }

            var emails = await emailsResponse.Content.ReadFromJsonAsync<List<GitHubEmailItem>>(cancellationToken: cancellationToken);
            var primary = emails?.FirstOrDefault(e => e.Primary && e.Verified)?.Email
                          ?? emails?.FirstOrDefault(e => e.Verified)?.Email;

            return primary;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "尝试拉取 GitHub 用户邮箱列表失败");
            return null;
        }
    }

    private sealed record GitHubTokenResponse
    {
        [JsonPropertyName("access_token")]
        public string? AccessToken { get; init; }

        [JsonPropertyName("token_type")]
        public string? TokenType { get; init; }

        [JsonPropertyName("scope")]
        public string? Scope { get; init; }

        [JsonPropertyName("error")]
        public string? Error { get; init; }

        [JsonPropertyName("error_description")]
        public string? ErrorDescription { get; init; }
    }

    private sealed record GitHubRawUserResponse
    {
        [JsonPropertyName("id")]
        public long Id { get; init; }

        [JsonPropertyName("login")]
        public string Login { get; init; } = string.Empty;

        [JsonPropertyName("name")]
        public string? Name { get; init; }

        [JsonPropertyName("avatar_url")]
        public string? AvatarUrl { get; init; }

        [JsonPropertyName("email")]
        public string? Email { get; init; }

        [JsonPropertyName("bio")]
        public string? Bio { get; init; }
    }

    private sealed record GitHubEmailItem
    {
        [JsonPropertyName("email")]
        public string Email { get; init; } = string.Empty;

        [JsonPropertyName("primary")]
        public bool Primary { get; init; }

        [JsonPropertyName("verified")]
        public bool Verified { get; init; }
    }
}
