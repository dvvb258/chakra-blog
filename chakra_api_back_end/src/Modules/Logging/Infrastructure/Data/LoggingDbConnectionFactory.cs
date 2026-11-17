using Logging.Application.Common;
using Npgsql;
using System.Data;

namespace Logging.Infrastructure.Data;

/// <summary>
/// Logging 模块 PostgreSQL 连接工厂
/// </summary>
public sealed class LoggingDbConnectionFactory : ILoggingDbConnectionFactory
{
    private readonly string _connectionString;

    /// <summary>
    /// Initializes a new instance of the <see cref="LoggingDbConnectionFactory"/> class.
    /// </summary>
    /// <param name="connectionString">连接字符串</param>
    public LoggingDbConnectionFactory(string connectionString)
    {
        _connectionString = connectionString;
    }

    /// <inheritdoc />
    public IDbConnection CreateConnection()
    {
        return new NpgsqlConnection(_connectionString);
    }
}
