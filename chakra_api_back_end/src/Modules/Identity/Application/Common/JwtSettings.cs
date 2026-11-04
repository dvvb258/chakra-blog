namespace Identity.Application.Common;

/// <summary>
/// JWT 身份认证配置选项
/// </summary>
public sealed class JwtSettings
{
    /// <summary>
    /// 配置节名称
    /// </summary>
    public const string SectionName = "JwtSettings";

    /// <summary>
    /// 签名安全密钥（至少32个字符，生产环境必须通过配置覆盖）
    /// </summary>
    public string Secret { get; set; } = "CHANGE_ME_BlogVerySecretKeyForJwtAuthenticationMustBeAtLeast32Chars!";

    /// <summary>
    /// 令牌颁发者
    /// </summary>
    public string Issuer { get; set; } = "chakra_api";

    /// <summary>
    /// 令牌受众
    /// </summary>
    public string Audience { get; set; } = "chakra_client";

    /// <summary>
    /// AccessToken 访问令牌过期时间（分钟，默认 120 分钟）
    /// </summary>
    public int AccessTokenExpirationMinutes { get; set; } = 120;

    /// <summary>
    /// RefreshToken 刷新令牌过期时间（天，默认 7 天）
    /// </summary>
    public int RefreshTokenExpirationDays { get; set; } = 7;
}
