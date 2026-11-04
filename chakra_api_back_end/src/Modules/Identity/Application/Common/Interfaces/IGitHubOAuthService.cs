using Identity.Application.Common.Models;

namespace Identity.Application.Common.Interfaces;

/// <summary>
/// GitHub OAuth 身份认证通信服务契约
/// </summary>
public interface IGitHubOAuthService
{
    /// <summary>
    /// 获取当前系统是否已启用并有效配置了 GitHub OAuth 服务
    /// </summary>
    bool IsEnabled { get; }

    /// <summary>
    /// 生成引导用户授权的 GitHub OAuth 目标跳转 URL
    /// </summary>
    /// <param name="state">状态令牌参数（防止 CSRF 攻击）</param>
    /// <returns>完整的授权跳转地址</returns>
    string GetAuthorizationUrl(string state);

    /// <summary>
    /// 使用 GitHub 授权回调 code 换取访问令牌并拉取用户基本信息
    /// </summary>
    /// <param name="code">GitHub 授权临时授权码</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>解析出的 GitHub 用户身份数据</returns>
    Task<GitHubUserInfo> GetUserInfoByCodeAsync(string code, CancellationToken cancellationToken = default);
}
