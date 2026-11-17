using Identity.Application.Common;
using Npgsql;
using System.Data;

namespace Identity.Infrastructure.Data;

/// <summary>
/// Identity 模块 PostgreSQL 连接工厂
/// </summary>
public sealed class IdentityDbConnectionFactory : IIdentityDbConnectionFactory
{
    private readonly string _connectionString;

    /// <summary>
    /// Initializes a new instance of the <see cref="IdentityDbConnectionFactory"/> class.
    /// </summary>
    /// <param name="connectionString">连接字符串</param>
    public IdentityDbConnectionFactory(string connectionString)
    {
        _connectionString = connectionString;
    }

    /// <inheritdoc />
    public IDbConnection CreateConnection()
    {
        return new NpgsqlConnection(_connectionString);
    }
}
