using Identity.Domain.Entities;

namespace Identity.Application.Common.Interfaces;

/// <summary>
/// JWT 令牌服务接口
/// </summary>
public interface IJwtTokenService
{
    /// <summary>
    /// 为指定用户生成 AccessToken 访问令牌字符串
    /// </summary>
    /// <param name="user">用户实体</param>
    /// <returns>JWT 字符串</returns>
    string GenerateAccessToken(User user);

    /// <summary>
    /// 生成加密强随机的 RefreshToken 刷新令牌字符串
    /// </summary>
    /// <returns>安全随机字符串</returns>
    string GenerateRefreshTokenString();

    /// <summary>
    /// 计算刷新令牌的不可逆存储哈希
    /// </summary>
    /// <param name="refreshToken">刷新令牌原文</param>
    /// <returns>SHA-256 十六进制哈希</returns>
    string HashRefreshToken(string refreshToken);

    /// <summary>
    /// 获取当前配置的 AccessToken 有效期（秒）
    /// </summary>
    /// <returns>有效秒数</returns>
    int GetAccessTokenExpirationSeconds();

    /// <summary>
    /// 获取当前配置的 RefreshToken 过期时间点（UTC）
    /// </summary>
    /// <returns>过期时间点</returns>
    DateTime GetRefreshTokenExpiration();
}
