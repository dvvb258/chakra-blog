using FluentAssertions;
using Identity.Application.Users.Queries;
using Identity.Domain.Enums;
using Identity.Infrastructure.Data;
using Npgsql;
using SharedKernel.Constants;
using Xunit;

namespace Chakra.Tests.Integration;

[Collection("postgres")]
public sealed class IdentityReadStoreIntegrationTests : PostgresIntegrationTestBase
{
    public IdentityReadStoreIntegrationTests(PostgresTestDatabase database)
        : base(database)
    {
    }

    [PostgresFact]
    public async Task ReadStore_ShouldResolvePermissionsMenusAndAccessState()
    {
        await SeedIdentityDataAsync();

        var factory = new IdentityDbConnectionFactory(ConnectionString);
        var store = new DapperIdentityReadStore(factory);

        (await store.HasPermissionAsync("editor", "article:manage")).Should().BeTrue();
        (await store.HasPermissionAsync("editor", "content:article:create")).Should().BeTrue();
        (await store.HasPermissionAsync("editor", "content:article:delete")).Should().BeFalse();

        var editorMenus = await store.GetMenusByRoleCodeAsync("EDITOR");
        editorMenus.Select(menu => menu.Id).Should().ContainInOrder(200L, 201L);
        editorMenus.Should().OnlyContain(menu => menu.PermissionCode.Length > 0);

        var activeMenus = await store.GetActiveMenusAsync();
        activeMenus.Select(menu => menu.Id).Should().BeEquivalentTo(new long[] { 100L, 200L, 201L });
        activeMenus.Select(menu => menu.Id).Should().NotContain(202L);

        (await store.IsAccessAllowedAsync(3001, "editor")).Should().BeTrue();
        (await store.IsAccessAllowedAsync(3002, "editor")).Should().BeFalse();
        (await store.IsAccessAllowedAsync(3003, "editor")).Should().BeFalse();
        (await store.GetActiveUserRoleAsync(3003)).Should().BeNull();
        (await store.GetActiveUserRoleAsync(3001)).Should().Be("Editor");
    }

    [PostgresFact]
    public async Task CurrentUserQuery_ShouldMapPasswordFlagAndAuthorizationCollections()
    {
        await SeedIdentityDataAsync();

        var factory = new IdentityDbConnectionFactory(ConnectionString);
        var store = new DapperIdentityReadStore(factory);
        var handler = new GetCurrentUserQueryHandler(factory, store);

        var result = await handler.Handle(
            new GetCurrentUserQuery(3001),
            CancellationToken.None);

        result.Username.Should().Be("editor");
        result.Role.Should().Be("Editor");
        result.HasPassword.Should().BeTrue();
        result.Menus.Should().Contain("/articles");
        result.Permissions.Should().Contain("article:manage");
        result.Permissions.Should().Contain("content:article:create");
    }

    [PostgresFact]
    public async Task UsersQuery_ShouldApplyKeywordRoleActiveAndPagingFilters()
    {
        await SeedIdentityDataAsync();

        var handler = new GetUsersQueryHandler(new IdentityDbConnectionFactory(ConnectionString));
        var activeResult = await handler.Handle(
            new GetUsersQuery { Keyword = "editor", Role = "Editor", IsActive = true },
            CancellationToken.None);

        activeResult.Total.Should().Be(1);
        activeResult.Items.Should().ContainSingle();
        activeResult.Items[0].Id.Should().Be(3001);
        activeResult.Items[0].HasPassword.Should().BeTrue();

        var inactiveResult = await handler.Handle(
            new GetUsersQuery { Role = "Editor", IsActive = false, PageSize = 1 },
            CancellationToken.None);

        inactiveResult.Total.Should().Be(1);
        inactiveResult.Items.Should().ContainSingle();
        inactiveResult.Items[0].Id.Should().Be(3002);
    }

    [PostgresFact]
    public async Task RolesQuery_ShouldReturnRoleMenuMappingsFromMultipleResultSets()
    {
        await SeedIdentityDataAsync();

        var handler = new GetRolesQueryHandler(new IdentityDbConnectionFactory(ConnectionString));
        var result = await handler.Handle(new GetRolesQuery(), CancellationToken.None);

        result.Should().HaveCount(2);
        var admin = result.Single(role => role.Code == RoleNames.Admin);
        var editor = result.Single(role => role.Code == RoleNames.Editor);

        admin.MenuIds.Should().BeEquivalentTo(new long[] { 100L, 200L, 201L });
        editor.MenuIds.Should().BeEquivalentTo(new long[] { 200L, 201L });
        admin.IsSystem.Should().BeTrue();
        editor.IsSystem.Should().BeTrue();
    }

    private async Task SeedIdentityDataAsync()
    {
        await using var connection = new NpgsqlConnection(ConnectionString);
        await connection.OpenAsync();

        await PostgresTestData.InsertRoleAsync(connection, 1, RoleNames.Admin, isSystem: true);
        await PostgresTestData.InsertRoleAsync(connection, 2, RoleNames.Editor, isSystem: true);

        await PostgresTestData.InsertMenuAsync(connection, 100, "Dashboard", "/", "dashboard:view", (int)MenuType.Menu, 1);
        await PostgresTestData.InsertMenuAsync(connection, 200, "Articles", "/articles", "article:manage", (int)MenuType.Menu, 2);
        await PostgresTestData.InsertMenuAsync(connection, 201, "Create article", "", "content:article:create", (int)MenuType.Button, 3);
        await PostgresTestData.InsertMenuAsync(connection, 202, "Deleted menu", "/deleted", "deleted:view", (int)MenuType.Menu, 4, isDeleted: true);

        await PostgresTestData.InsertRoleMenuAsync(connection, 1001, 1, 100);
        await PostgresTestData.InsertRoleMenuAsync(connection, 1002, 2, 200);
        await PostgresTestData.InsertRoleMenuAsync(connection, 1003, 2, 201);

        await PostgresTestData.InsertUserAsync(connection, 3001, "editor", RoleNames.Editor);
        await PostgresTestData.InsertUserAsync(connection, 3002, "inactive", RoleNames.Editor, isActive: false);
        await PostgresTestData.InsertUserAsync(connection, 3003, "deleted", RoleNames.Editor, isDeleted: true);
    }
}
