using Identity.Application.Common.Interfaces;
using Microsoft.Extensions.Caching.Memory;

namespace Identity.Infrastructure.Services;

/// <summary>
/// 基于身份数据库校验访问令牌中的用户状态与角色（内置短效内存缓存以削平并发查库峰值）
/// </summary>
public sealed class UserAccessValidator : IUserAccessValidator
{
    private static readonly TimeSpan CacheDuration = TimeSpan.FromSeconds(60);
    private readonly IIdentityReadStore _readStore;
    private readonly IMemoryCache _cache;

    /// <summary>
    /// Initializes a new instance of the <see cref="UserAccessValidator"/> class.
    /// </summary>
    /// <param name="readStore">Identity 读模型存储</param>
    /// <param name="cache">内存缓存</param>
    public UserAccessValidator(IIdentityReadStore readStore, IMemoryCache cache)
    {
        _readStore = readStore;
        _cache = cache;
    }

    /// <inheritdoc />
    public async Task<bool> IsAccessAllowedAsync(long userId, string role, CancellationToken cancellationToken = default)
    {
        if (userId <= 0 || string.IsNullOrWhiteSpace(role))
        {
            return false;
        }

        var normalizedRole = role.Trim().ToLowerInvariant();
        var cacheKey = $"user_access:{userId}:{normalizedRole}";

        if (_cache.TryGetValue(cacheKey, out bool isAllowed))
        {
            return isAllowed;
        }

        isAllowed = await _readStore.IsAccessAllowedAsync(userId, normalizedRole, cancellationToken);

        // 仅对有效鉴权结果进行 60 秒短效缓存（未通过的不缓存，以便状态恢复后能立即生效）
        if (isAllowed)
        {
            _cache.Set(cacheKey, true, CacheDuration);
        }

        return isAllowed;
    }
}
