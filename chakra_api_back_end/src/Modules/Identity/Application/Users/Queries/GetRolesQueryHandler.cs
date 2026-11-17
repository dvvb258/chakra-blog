using Dapper;
using Identity.Application.Common;
using Identity.Application.Users.DTOs;
using SharedKernel.Application.CQRS;
using SharedKernel.Constants;

namespace Identity.Application.Users.Queries;

/// <summary>
/// 查询系统可用角色列表处理器
/// </summary>
public sealed class GetRolesQueryHandler : IQueryHandler<GetRolesQuery, List<RoleDto>>
{
    private readonly IIdentityDbConnectionFactory _connectionFactory;

    /// <summary>
    /// Initializes a new instance of the <see cref="GetRolesQueryHandler"/> class.
    /// </summary>
    /// <param name="connectionFactory">数据库连接工厂</param>
    public GetRolesQueryHandler(IIdentityDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    /// <inheritdoc />
    public async Task<List<RoleDto>> Handle(GetRolesQuery request, CancellationToken cancellationToken)
    {
        using var connection = _connectionFactory.CreateConnection();

        const string batchSql = """
            SELECT id, code, name, description, is_system
            FROM roles
            WHERE is_del = false
            ORDER BY id ASC;

            SELECT role_id, menu_id
            FROM role_menus
            WHERE is_del = false;

            SELECT id
            FROM menus
            WHERE is_del = false;
            """;

        using var multi = await connection.QueryMultipleAsync(
            new CommandDefinition(batchSql, cancellationToken: cancellationToken));

        var roles = (await multi.ReadAsync<RoleDto>()).ToList();
        var roleMenus = (await multi.ReadAsync<RoleMenuRow>()).ToList();
        var allMenus = (await multi.ReadAsync<long>()).ToList();

        var result = roles.Select(r =>
        {
            List<long> menuIds;

            // 核心法则：超级管理员在底层天然具备全量权限菜单
            if (string.Equals(r.Code, RoleNames.Admin, StringComparison.OrdinalIgnoreCase))
            {
                menuIds = allMenus;
            }
            else
            {
                menuIds = roleMenus.Where(rm => rm.RoleId == r.Id).Select(rm => rm.MenuId).ToList();
            }

            return new RoleDto
            {
                Id = r.Id,
                Code = r.Code,
                Name = r.Name,
                Description = r.Description,
                IsSystem = r.IsSystem || string.Equals(r.Code, RoleNames.Admin, StringComparison.OrdinalIgnoreCase),
                MenuIds = menuIds,
            };
        }).ToList();

        return result;
    }

    private sealed record RoleMenuRow
    {
        public long RoleId { get; init; }
        public long MenuId { get; init; }
    }
}
