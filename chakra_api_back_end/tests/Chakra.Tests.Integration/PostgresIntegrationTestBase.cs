using Xunit;

namespace Chakra.Tests.Integration;

public abstract class PostgresIntegrationTestBase : IAsyncLifetime
{
    protected PostgresIntegrationTestBase(PostgresTestDatabase database)
    {
        Database = database;
    }

    protected PostgresTestDatabase Database { get; }

    protected string ConnectionString => Database.ConnectionString;

    public Task InitializeAsync()
    {
        return Database.ResetAsync();
    }

    public Task DisposeAsync()
    {
        return Task.CompletedTask;
    }
}
