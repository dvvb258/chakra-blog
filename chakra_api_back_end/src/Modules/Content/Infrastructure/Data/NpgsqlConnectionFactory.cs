using Content.Application.Common;
using Npgsql;
using System.Data;

namespace Content.Infrastructure.Data;

/// <summary>
/// Content 模块 PostgreSQL 连接工厂
/// </summary>
public sealed class NpgsqlConnectionFactory : IContentDbConnectionFactory
{
    private readonly string _connectionString;

    /// <summary>
    /// Initializes a new instance of the <see cref="NpgsqlConnectionFactory"/> class.
    /// </summary>
    /// <param name="connectionString">连接字符串</param>
    public NpgsqlConnectionFactory(string connectionString)
    {
        _connectionString = connectionString;
    }

    /// <inheritdoc />
    public IDbConnection CreateConnection()
    {
        return new NpgsqlConnection(_connectionString);
    }
}
