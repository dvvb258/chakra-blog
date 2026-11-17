using Dapper;
using Identity.Application.Common;
using Identity.Application.Common.Interfaces;
using Identity.Application.Users.DTOs;
using SharedKernel.Application.CQRS;
using SharedKernel.Application.Exceptions;

namespace Identity.Application.Users.Queries;

/// <summary>
/// 获取当前登录用户信息查询处理器
/// </summary>
public sealed class GetCurrentUserQueryHandler : IQueryHandler<GetCurrentUserQuery, CurrentUserDto>
{
    private readonly IIdentityDbConnectionFactory _connectionFactory;
    private readonly IIdentityReadStore _readStore;

    /// <summary>
    /// Initializes a new instance of the <see cref="GetCurrentUserQueryHandler"/> class.
    /// </summary>
    /// <param name="connectionFactory">数据库连接工厂</param>
    /// <param name="readStore">Identity 读模型存储</param>
    public GetCurrentUserQueryHandler(
        IIdentityDbConnectionFactory connectionFactory,
        IIdentityReadStore readStore)
    {
        _connectionFactory = connectionFactory;
        _readStore = readStore;
    }

    /// <inheritdoc />
    public async Task<CurrentUserDto> Handle(GetCurrentUserQuery request, CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT id, username, nickname, email, avatar_url, role,
                   (password_hash IS NOT NULL AND password_hash <> '') AS has_password,
                   github_id, last_login_at
            FROM users
            WHERE id = @UserId AND is_del = false
            """;

        CurrentUserDto? user;

        using (var connection = _connectionFactory.CreateConnection())
        {
            var command = new CommandDefinition(sql, new { UserId = request.UserId }, cancellationToken: cancellationToken);
            user = await connection.QueryFirstOrDefaultAsync<CurrentUserDto>(command);
        }

        if (user is null)
        {
            throw new NotFoundException("User", request.UserId);
        }

        var (menuPaths, permissions) = await _readStore.GetUserMenusAndPermissionsAsync(user.Role, cancellationToken);

        return user with { Menus = menuPaths, Permissions = permissions };
    }
}
