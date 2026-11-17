using FluentAssertions;
using Identity.Application.Common;
using Identity.Application.Common.Interfaces;
using Identity.Application.Common.Models;
using Identity.Domain.Enums;
using Identity.Infrastructure.Services;
using Microsoft.Extensions.Caching.Memory;
using Xunit;

namespace Chakra.Tests.Unit;

public class PermissionCheckerCacheTests
{
    private sealed class FakeReadStore : IIdentityReadStore
    {
        public HashSet<string> GrantedPermissions { get; } = new(StringComparer.OrdinalIgnoreCase);

        public List<MenuRow> ActiveMenus { get; } = [];

        public Dictionary<string, List<MenuRow>> RoleMenus { get; } = new(StringComparer.OrdinalIgnoreCase);

        public bool AccessAllowed { get; set; }

        public string? UserRole { get; set; }

        public int PermissionLookups { get; private set; }

        public Task<bool> HasPermissionAsync(string roleCode, string permissionCode, CancellationToken cancellationToken = default)
        {
            PermissionLookups++;
            return Task.FromResult(GrantedPermissions.Contains(permissionCode));
        }

        public Task<bool> IsAccessAllowedAsync(long userId, string roleCode, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(AccessAllowed);
        }

        public Task<string?> GetActiveUserRoleAsync(long userId, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(UserRole);
        }

        public Task<IReadOnlyList<MenuRow>> GetActiveMenusAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyList<MenuRow>>(ActiveMenus);
        }

        public Task<IReadOnlyList<MenuRow>> GetMenusByRoleCodeAsync(string roleCode, CancellationToken cancellationToken = default)
        {
            IReadOnlyList<MenuRow> menus = RoleMenus.TryGetValue(roleCode, out var value) ? value : [];
            return Task.FromResult(menus);
        }
    }

    [Fact]
    public async Task HasPermissionAsync_ShouldCacheStoreLookup_WithinTtl()
    {
        var store = new FakeReadStore();
        store.GrantedPermissions.Add("article:manage");
        using var cache = new MemoryCache(new MemoryCacheOptions());
        var checker = new PermissionChecker(store, cache);

        (await checker.HasPermissionAsync("editor", "article:manage")).Should().BeTrue();
        (await checker.HasPermissionAsync("Editor", "article:manage")).Should().BeTrue();

        store.PermissionLookups.Should().Be(1);
    }

    [Fact]
    public async Task HasPermissionAsync_ShouldReturnFalse_WhenStoreDenies()
    {
        var store = new FakeReadStore();
        using var cache = new MemoryCache(new MemoryCacheOptions());
        var checker = new PermissionChecker(store, cache);

        (await checker.HasPermissionAsync("editor", "series:manage")).Should().BeFalse();
    }

    [Fact]
    public async Task HasPermissionAsync_ShouldAlwaysGrantAdmin_WithoutStoreLookup()
    {
        var store = new FakeReadStore();
        using var cache = new MemoryCache(new MemoryCacheOptions());
        var checker = new PermissionChecker(store, cache);

        (await checker.HasPermissionAsync("Admin", "any:permission")).Should().BeTrue();
        store.PermissionLookups.Should().Be(0);
    }

    [Fact]
    public async Task HasPermissionAsync_ShouldRejectEmptyRoleOrPermission()
    {
        var store = new FakeReadStore();
        using var cache = new MemoryCache(new MemoryCacheOptions());
        var checker = new PermissionChecker(store, cache);

        (await checker.HasPermissionAsync(string.Empty, "article:manage")).Should().BeFalse();
        (await checker.HasPermissionAsync("editor", " ")).Should().BeFalse();
        store.PermissionLookups.Should().Be(0);
    }

    [Fact]
    public async Task UserAccessValidator_ShouldReturnTrueAndCache_WhenStoreAllows()
    {
        var store = new FakeReadStore { AccessAllowed = true };
        using var cache = new MemoryCache(new MemoryCacheOptions());
        var validator = new UserAccessValidator(store, cache);

        (await validator.IsAccessAllowedAsync(1001, "Editor")).Should().BeTrue();

        // 存储层状态变化后，缓存未过期前仍返回缓存结果
        store.AccessAllowed = false;
        (await validator.IsAccessAllowedAsync(1001, "Editor")).Should().BeTrue();
    }

    [Fact]
    public async Task UserAccessValidator_ShouldReturnFalse_WhenStoreDeniesOrArgumentsInvalid()
    {
        var store = new FakeReadStore { AccessAllowed = false };
        using var cache = new MemoryCache(new MemoryCacheOptions());
        var validator = new UserAccessValidator(store, cache);

        (await validator.IsAccessAllowedAsync(1001, "Editor")).Should().BeFalse();
        (await validator.IsAccessAllowedAsync(0, "Editor")).Should().BeFalse();
        (await validator.IsAccessAllowedAsync(1001, "  ")).Should().BeFalse();
    }

    [Fact]
    public async Task GetUserMenusAndPermissionsAsync_ShouldExcludeButtonsFromMenus_ButKeepTheirPermissions()
    {
        var store = new FakeReadStore();
        store.RoleMenus["custom_editor"] =
        [
            new MenuRow { Id = 1, Name = "文章管理", Path = "/articles", PermissionCode = "article:manage", Sort = 1, Type = (int)MenuType.Menu },
            new MenuRow { Id = 2, Name = "新建文章", Path = string.Empty, PermissionCode = "content:article:create", Sort = 2, Type = (int)MenuType.Button },
        ];

        var (menus, permissions) = await store.GetUserMenusAndPermissionsAsync("custom_editor");

        menus.Should().ContainSingle().Which.Should().Be("/articles");
        permissions.Should().Contain("article:manage");
        permissions.Should().Contain("content:article:create");
    }

    [Fact]
    public async Task GetUserMenusAndPermissionsAsync_ShouldReturnAllMenus_ForAdmin()
    {
        var store = new FakeReadStore();
        store.ActiveMenus.AddRange(
        [
            new MenuRow { Id = 1, Name = "文章管理", Path = "/articles", PermissionCode = "article:manage", Sort = 1, Type = (int)MenuType.Menu },
            new MenuRow { Id = 2, Name = "系统设置", Path = "/settings", PermissionCode = "settings:manage", Sort = 2, Type = (int)MenuType.Menu },
        ]);

        var (menus, permissions) = await store.GetUserMenusAndPermissionsAsync("Admin");

        menus.Should().BeEquivalentTo(new[] { "/articles", "/settings" });
        permissions.Should().BeEquivalentTo(new[] { "article:manage", "settings:manage" });
    }
}
