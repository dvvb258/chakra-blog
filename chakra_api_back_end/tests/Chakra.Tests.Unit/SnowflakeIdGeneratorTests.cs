using FluentAssertions;
using SharedKernel.Domain;
using Xunit;

namespace Chakra.Tests.Unit;

public class SnowflakeIdGeneratorTests
{
    [Fact]
    public void NextId_ShouldGeneratePositiveUniqueIds()
    {
        // Act
        var id1 = SnowflakeIdGenerator.NextId();
        var id2 = SnowflakeIdGenerator.NextId();

        // Assert
        id1.Should().BePositive();
        id2.Should().BePositive();
        id1.Should().NotBe(id2);
    }

    [Fact]
    public void NextId_ConcurrentExecution_ShouldNotGenerateDuplicates()
    {
        // Arrange
        const int count = 1000;
        var ids = new long[count];

        // Act
        Parallel.For(0, count, i =>
        {
            ids[i] = SnowflakeIdGenerator.NextId();
        });

        // Assert
        ids.Distinct().Count().Should().Be(count);
    }
}
