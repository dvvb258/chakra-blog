using Content.Application.Common;
using Dapper;

namespace Content.Infrastructure.Services;

public sealed class PostgresSystemStatsTracker(IContentDbConnectionFactory connectionFactory) : ISystemStatsTracker
{
    public async Task IncrementSystemVisitsAsync(CancellationToken cancellationToken = default)
    {
        using var connection = connectionFactory.CreateConnection();
        const string sql = """
            INSERT INTO system_stats (key, value) VALUES ('total_visits', 1)
            ON CONFLICT (key) DO UPDATE SET value = system_stats.value + 1;
            """;
        await connection.ExecuteAsync(new CommandDefinition(sql, cancellationToken: cancellationToken));
    }

    public async Task<long> GetSystemVisitsAsync(CancellationToken cancellationToken = default)
    {
        using var connection = connectionFactory.CreateConnection();
        const string sql = "SELECT COALESCE((SELECT value FROM system_stats WHERE key = 'total_visits'), 0);";
        return await connection.ExecuteScalarAsync<long>(new CommandDefinition(sql, cancellationToken: cancellationToken));
    }
}
