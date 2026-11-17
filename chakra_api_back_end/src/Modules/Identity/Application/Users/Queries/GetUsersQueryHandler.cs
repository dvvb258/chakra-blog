using Dapper;
using Identity.Application.Common;
using Identity.Application.Users.DTOs;
using SharedKernel.Application;
using SharedKernel.Application.CQRS;

namespace Identity.Application.Users.Queries;

/// <summary>
/// 分页查询系统用户列表处理器
/// </summary>
public sealed class GetUsersQueryHandler : IQueryHandler<GetUsersQuery, PagedResult<UserListItemDto>>
{
    private readonly IIdentityDbConnectionFactory _connectionFactory;

    /// <summary>
    /// Initializes a new instance of the <see cref="GetUsersQueryHandler"/> class.
    /// </summary>
    /// <param name="connectionFactory">数据库连接工厂</param>
    public GetUsersQueryHandler(IIdentityDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    /// <inheritdoc />
    public async Task<PagedResult<UserListItemDto>> Handle(GetUsersQuery request, CancellationToken cancellationToken)
    {
        using var connection = _connectionFactory.CreateConnection();

        var conditions = new List<string> { "is_del = false" };

        if (!string.IsNullOrWhiteSpace(request.Keyword))
        {
            conditions.Add("(username ILIKE @Keyword OR nickname ILIKE @Keyword OR email ILIKE @Keyword)");
        }

        if (!string.IsNullOrWhiteSpace(request.Role))
        {
            conditions.Add("role = @Role");
        }

        if (request.IsActive.HasValue)
        {
            conditions.Add("is_active = @IsActive");
        }

        var whereClause = "WHERE " + string.Join(" AND ", conditions);

        var countSql = $"""
            SELECT COUNT(*)
            FROM users
            {whereClause}
            """;
        var dataSql = $"""
            SELECT id, username, nickname, email, avatar_url, role, is_active,
                   (password_hash IS NOT NULL AND password_hash <> '') AS has_password,
                   github_id, last_login_at, created_at
            FROM users
            {whereClause}
            ORDER BY created_at DESC, id DESC
            LIMIT @PageSize OFFSET @Offset
            """;

        var batchSql = $"""
            {countSql};

            {dataSql};
            """;

        var parameters = new
        {
            Keyword = $"%{request.Keyword}%",
            Role = request.Role?.Trim(),
            IsActive = request.IsActive,
            Offset = (request.PageIndex - 1) * request.PageSize,
            PageSize = request.PageSize,
        };

        using var multi = await connection.QueryMultipleAsync(
            new CommandDefinition(batchSql, parameters, cancellationToken: cancellationToken));

        var total = await multi.ReadFirstAsync<int>();
        var items = (await multi.ReadAsync<UserListItemDto>()).ToList();

        return new PagedResult<UserListItemDto>
        {
            Items = items,
            Total = total,
            PageIndex = request.PageIndex,
            PageSize = request.PageSize
        };
    }
}
