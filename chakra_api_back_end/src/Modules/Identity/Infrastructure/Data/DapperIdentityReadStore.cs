using Dapper;
using Identity.Application.Common;
using Identity.Application.Common.Interfaces;
using Identity.Application.Common.Models;

namespace Identity.Infrastructure.Data;

/// <summary>
/// Identity 模块基于 Dapper 的读模型存储实现
/// </summary>
public sealed class DapperIdentityReadStore : IIdentityReadStore
{
    private const string PermissionSql = """
        SELECT EXISTS (
            SELECT 1
            FROM role_menus rm
            JOIN roles r ON r.id = rm.role_id
            JOIN menus m ON m.id = rm.menu_id
            WHERE rm.is_del = false
              AND r.is_del = false
              AND m.is_del = false
              AND lower(r.code) = @RoleCode
              AND m.permission_code IS NOT NULL
              AND lower(m.permission_code) = @PermissionCode
        )
        """;

    private const string AccessAllowedSql = """
        SELECT EXISTS (
            SELECT 1
            FROM users
            WHERE id = @UserId
              AND is_del = false
              AND is_active = true
              AND lower(role) = @RoleCode
        )
        """;

    private const string ActiveUserRoleSql = """
        SELECT role
        FROM users
        WHERE id = @UserId AND is_del = false
        """;

    private const string ActiveMenusSql = """
        SELECT id, parent_id, name, path, permission_code, icon, sort, is_hidden, type
        FROM menus
        WHERE is_del = false
        ORDER BY sort ASC, id ASC
        """;

    private const string RoleMenusSql = """
        SELECT m.id, m.parent_id, m.name, m.path, m.permission_code, m.icon, m.sort, m.is_hidden, m.type
        FROM menus m
        JOIN role_menus rm ON rm.menu_id = m.id AND rm.is_del = false
        JOIN roles r ON r.id = rm.role_id AND r.is_del = false
        WHERE m.is_del = false AND lower(r.code) = @RoleCode
        ORDER BY m.sort ASC, m.id ASC
        """;

    private readonly IIdentityDbConnectionFactory _connectionFactory;

    /// <summary>
    /// Initializes a new instance of the <see cref="DapperIdentityReadStore"/> class.
    /// </summary>
    /// <param name="connectionFactory">数据库连接工厂</param>
    public DapperIdentityReadStore(IIdentityDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    /// <inheritdoc />
    public async Task<bool> HasPermissionAsync(string roleCode, string permissionCode, CancellationToken cancellationToken = default)
    {
        using var connection = _connectionFactory.CreateConnection();

        var command = new CommandDefinition(
            PermissionSql,
            new { RoleCode = roleCode, PermissionCode = permissionCode },
            cancellationToken: cancellationToken);

        return await connection.QuerySingleAsync<bool>(command);
    }

    /// <inheritdoc />
    public async Task<bool> IsAccessAllowedAsync(long userId, string roleCode, CancellationToken cancellationToken = default)
    {
        using var connection = _connectionFactory.CreateConnection();

        var command = new CommandDefinition(
            AccessAllowedSql,
            new { UserId = userId, RoleCode = roleCode },
            cancellationToken: cancellationToken);

        return await connection.QuerySingleAsync<bool>(command);
    }

    /// <inheritdoc />
    public async Task<string?> GetActiveUserRoleAsync(long userId, CancellationToken cancellationToken = default)
    {
        using var connection = _connectionFactory.CreateConnection();

        var command = new CommandDefinition(
            ActiveUserRoleSql,
            new { UserId = userId },
            cancellationToken: cancellationToken);

        return await connection.QueryFirstOrDefaultAsync<string>(command);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<MenuRow>> GetActiveMenusAsync(CancellationToken cancellationToken = default)
    {
        using var connection = _connectionFactory.CreateConnection();

        var command = new CommandDefinition(ActiveMenusSql, cancellationToken: cancellationToken);
        var rows = await connection.QueryAsync<MenuRow>(command);

        return rows.AsList();
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<MenuRow>> GetMenusByRoleCodeAsync(string roleCode, CancellationToken cancellationToken = default)
    {
        using var connection = _connectionFactory.CreateConnection();

        var command = new CommandDefinition(
            RoleMenusSql,
            new { RoleCode = roleCode.Trim().ToLowerInvariant() },
            cancellationToken: cancellationToken);

        var rows = await connection.QueryAsync<MenuRow>(command);

        return rows.AsList();
    }
}
