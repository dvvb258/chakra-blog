using Identity.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using SharedKernel.Application.Interfaces;
using SharedKernel.Constants;

namespace Identity.Infrastructure.Services;

/// <summary>
/// 角色权限验证服务落地实现
/// </summary>
public sealed class PermissionChecker : IPermissionChecker
{
    private readonly IdentityDbContext _dbContext;

    /// <summary>
    /// Initializes a new instance of the <see cref="PermissionChecker"/> class.
    /// </summary>
    /// <param name="dbContext">身份认证数据库上下文</param>
    public PermissionChecker(IdentityDbContext dbContext)
    {
        _dbContext = dbContext;
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

        // 每次从数据库读取，确保角色和菜单权限变更在所有 Pod 立即生效
        var normalizedRole = roleCode.Trim().ToLowerInvariant();
        var normalizedPermission = permissionCode.Trim().ToLowerInvariant();

        // 排除包含子节点的父级容器菜单，普通角色的具体业务权限必须来自叶子节点（按钮/独立页面）
        var parentMenuIds = await _dbContext.Menus
            .AsNoTracking()
            .Where(m => !m.IsDeleted && m.ParentId != null)
            .Select(m => m.ParentId!.Value)
            .Distinct()
            .ToListAsync(cancellationToken);

        return await (from rm in _dbContext.RoleMenus.AsNoTracking()
                      join r in _dbContext.Roles.AsNoTracking() on rm.RoleId equals r.Id
                      join m in _dbContext.Menus.AsNoTracking() on rm.MenuId equals m.Id
                      where !rm.IsDeleted && !r.IsDeleted && !m.IsDeleted
                            && !parentMenuIds.Contains(m.Id)
                            && r.Code.ToLower() == normalizedRole
                            && m.PermissionCode.ToLower() == normalizedPermission
                      select rm.Id)
                      .AnyAsync(cancellationToken);
    }
}
