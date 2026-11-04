using SharedKernel.Domain;

namespace Identity.Domain.Entities;

/// <summary>
/// 刷新令牌实体（支持令牌轮换与主动注销）
/// </summary>
public class RefreshToken : Entity
{
    /// <summary>
    /// 所属用户ID
    /// </summary>
    public long UserId { get; private set; }

    /// <summary>
    /// 令牌 SHA-256 哈希，不保存令牌原文
    /// </summary>
    public string TokenHash { get; private set; } = string.Empty;

    /// <summary>
    /// 过期时间（UTC）
    /// </summary>
    public DateTime ExpiresAt { get; private set; }

    /// <summary>
    /// 是否已被主动撤回/注销
    /// </summary>
    public bool IsRevoked { get; private set; }

    /// <summary>
    /// 撤回时间（UTC）
    /// </summary>
    public DateTime? RevokedAt { get; private set; }

    /// <summary>
    /// 轮换后新令牌的 SHA-256 哈希
    /// </summary>
    public string? ReplacedByTokenHash { get; private set; }

    /// <summary>
    /// 创建该令牌时的客户端IP
    /// </summary>
    public string? CreatedByIp { get; private set; }

    /// <summary>
    /// 令牌是否已过期
    /// </summary>
    public bool IsExpired => DateTime.UtcNow >= ExpiresAt;

    /// <summary>
    /// 令牌当前是否处于有效可刷新状态
    /// </summary>
    public bool IsActive => !IsRevoked && !IsExpired;

    /// <summary>
    /// EF Core 反射使用的私有无参构造函数
    /// </summary>
    private RefreshToken()
        : base()
    {
    }

    /// <summary>
    /// 创建新的刷新令牌
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="tokenHash">令牌 SHA-256 哈希</param>
    /// <param name="expiresAt">过期时间</param>
    /// <param name="createdByIp">客户端IP</param>
    public RefreshToken(long userId, string tokenHash, DateTime expiresAt, string? createdByIp = null)
        : base(SnowflakeIdGenerator.NextId())
    {
        if (userId <= 0)
        {
            throw new ArgumentException("用户ID非法", nameof(userId));
        }

        if (string.IsNullOrWhiteSpace(tokenHash))
        {
            throw new ArgumentException("令牌哈希不能为空", nameof(tokenHash));
        }

        UserId = userId;
        TokenHash = tokenHash;
        ExpiresAt = expiresAt;
        CreatedByIp = createdByIp;
        IsRevoked = false;
    }

    /// <summary>
    /// 撤销/作废当前刷新令牌（支持令牌轮换）
    /// </summary>
    /// <param name="replacedByTokenHash">替换该令牌的新 Token 哈希</param>
    public void Revoke(string? replacedByTokenHash = null)
    {
        IsRevoked = true;
        RevokedAt = DateTime.UtcNow;
        ReplacedByTokenHash = replacedByTokenHash;
        SetUpdatedAt();
    }
}
