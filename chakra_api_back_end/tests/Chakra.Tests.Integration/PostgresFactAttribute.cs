using Xunit;

namespace Chakra.Tests.Integration;

public sealed class PostgresFactAttribute : FactAttribute
{
    public const string EnvironmentVariable = "CHAKRA_TEST_POSTGRES";

    public PostgresFactAttribute()
    {
        if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable(EnvironmentVariable)))
        {
            Skip = $"Set {EnvironmentVariable} to a PostgreSQL admin connection string to run integration tests.";
        }
    }
}
