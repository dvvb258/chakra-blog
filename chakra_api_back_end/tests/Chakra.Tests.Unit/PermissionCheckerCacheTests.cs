using FluentAssertions;
using Identity.Domain.Entities;
using Identity.Infrastructure.Data;
using Identity.Infrastructure.Services;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Xunit;

namespace Chakra.Tests.Unit;

public class PermissionCheckerCacheTests
{
    private sealed class FakePublisher : IPublisher
    {
        public Task Publish(object notification, CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task Publish<TNotification>(TNotification notification, CancellationToken cancellationToken = default)
            where TNotification : INotification => Task.CompletedTask;
    }

    private static IdentityDbContext CreateInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<IdentityDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        return new IdentityDbContext(options, new FakePublisher());
    }

    private static PermissionChecker CreateChecker(IdentityDbContext db) =>
        new(db, new MemoryCache(new MemoryCacheOptions()));

    [Fact]
    public async Task HasPermissionAsync_ShouldGrant_WhenRoleOwnsLeafMenuPermission()
    {
        await using var db = CreateInMemoryDbContext();
        var role = new Role("editor", "编辑");
        var menu = new Menu("文章管理", "/articles", "article:manage");

        db.Roles.Add(role);
        db.Menus.Add(menu);
        await db.SaveChangesAsync();

        db.RoleMenus.Add(new RoleMenu(role.Id, menu.Id));
        await db.SaveChangesAsync();

        var checker = CreateChecker(db);

        (await checker.HasPermissionAsync("Editor", "article:manage")).Should().BeTrue();
        (await checker.HasPermissionAsync("editor", "series:manage")).Should().BeFalse();
    }

    [Fact]
    public async Task HasPermissionAsync_ShouldAlwaysGrantAdmin_WithoutDatabaseLookup()
    {
        await using var db = CreateInMemoryDbContext();
        var checker = CreateChecker(db);

        (await checker.HasPermissionAsync("Admin", "any:permission")).Should().BeTrue();
    }

    [Fact]
    public async Task HasPermissionAsync_ShouldServeFromCache_WithinTtl()
    {
        await using var db = CreateInMemoryDbContext();
        var role = new Role("editor", "编辑");
        var menu = new Menu("文章管理", "/articles", "article:manage");

        db.Roles.Add(role);
        db.Menus.Add(menu);
        await db.SaveChangesAsync();

        var roleMenu = new RoleMenu(role.Id, menu.Id);
        db.RoleMenus.Add(roleMenu);
        await db.SaveChangesAsync();

        var checker = CreateChecker(db);
        (await checker.HasPermissionAsync("editor", "article:manage")).Should().BeTrue();

        // 撤销授权后，缓存未过期前仍返回缓存结果（验证短 TTL 缓存确实生效）
        db.RoleMenus.Remove(roleMenu);
        await db.SaveChangesAsync();

        (await checker.HasPermissionAsync("editor", "article:manage")).Should().BeTrue();
    }

    [Fact]
    public async Task HasPermissionAsync_ShouldRejectEmptyRoleOrPermission()
    {
        await using var db = CreateInMemoryDbContext();
        var checker = CreateChecker(db);

        (await checker.HasPermissionAsync(string.Empty, "article:manage")).Should().BeFalse();
        (await checker.HasPermissionAsync("editor", " ")).Should().BeFalse();
    }
}
