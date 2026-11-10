using Identity.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using SharedKernel.Application.Interfaces;
using SharedKernel.Constants;

namespace Identity.Infrastructure.Services;

/// <summary>
/// 角色权限验证服务落地实现（带短 TTL 进程内缓存，降低每请求的数据库往返次数）
/// </summary>
public sealed class PermissionChecker : IPermissionChecker
{
    /// <summary>
    /// 权限校验结果缓存时长：足够削峰，又能让角色/菜单变更在可接受延迟内生效
    /// </summary>
    private static readonly TimeSpan CacheDuration = TimeSpan.FromSeconds(30);

    private readonly IdentityDbContext _dbContext;
    private readonly IMemoryCache _cache;

    /// <summary>
    /// Initializes a new instance of the <see cref="PermissionChecker"/> class.
    /// </summary>
    /// <param name="dbContext">身份认证数据库上下文</param>
    /// <param name="cache">进程内缓存</param>
    public PermissionChecker(IdentityDbContext dbContext, IMemoryCache cache)
    {
        _dbContext = dbContext;
        _cache = cache;
    }

    /// <inheritdoc />
    public async Task<bool> HasPermissionAsync(string roleCode, string permissionCode, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(roleCode) || string.IsNullOrWhiteSpace(permissionCode))
        {
            return false;
        }

        // 1. 超级管理员拥有全量通配特权（Bypass 模式）
        if (string.Equals(roleCode, RoleNames.Admin, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        var normalizedRole = roleCode.Trim().ToLowerInvariant();
        var normalizedPermission = permissionCode.Trim().ToLowerInvariant();
        var cacheKey = $"permission:{normalizedRole}:{normalizedPermission}";

        if (_cache.TryGetValue(cacheKey, out bool cached))
        {
            return cached;
        }

        // 2. 缓存未命中时查询数据库；结果缓存 30 秒，角色/菜单变更最迟 30 秒后在所有 Pod 生效
        var hasPermission = await (from rm in _dbContext.RoleMenus.AsNoTracking()
                                   join r in _dbContext.Roles.AsNoTracking() on rm.RoleId equals r.Id
                                   join m in _dbContext.Menus.AsNoTracking() on rm.MenuId equals m.Id
                                   where !rm.IsDeleted && !r.IsDeleted && !m.IsDeleted
                                         && r.Code.ToLower() == normalizedRole
                                         && m.PermissionCode != null && m.PermissionCode.ToLower() == normalizedPermission
                                   select rm.Id)
                                   .AnyAsync(cancellationToken);

        _cache.Set(cacheKey, hasPermission, CacheDuration);

        return hasPermission;
    }
}
