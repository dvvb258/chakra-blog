using Xunit;

namespace Chakra.Tests.Integration;

[CollectionDefinition("postgres")]
public sealed class PostgresCollection : ICollectionFixture<PostgresTestDatabase>
{
}
