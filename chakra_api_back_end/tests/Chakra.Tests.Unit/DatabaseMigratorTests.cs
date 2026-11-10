using ApiHost.Infrastructure;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Chakra.Tests.Unit;

public class DatabaseMigratorTests
{
    [Fact]
    public void CleanSql_ShouldRemoveStandaloneBeginAndCommit()
    {
        // Arrange
        const string sql = """
            -- Header comment
            BEGIN;

            CREATE TABLE IF NOT EXISTS example (
                id BIGINT PRIMARY KEY
            );

            INSERT INTO example (id) VALUES (1);

            COMMIT;
            """;

        // Act
        var cleaned = DatabaseMigrator.CleanSql(sql);

        // Assert
        cleaned.Should().NotContain("BEGIN;");
        cleaned.Should().NotContain("COMMIT;");
        cleaned.Should().Contain("CREATE TABLE IF NOT EXISTS example");
        cleaned.Should().Contain("INSERT INTO example (id) VALUES (1);");
    }

    [Fact]
    public async Task MigrateAsync_WithEmptyConnectionString_ShouldReturnSafely()
    {
        // Act & Assert (should not throw when connection string is empty)
        await DatabaseMigrator.MigrateAsync(string.Empty, NullLogger.Instance);
        await DatabaseMigrator.MigrateAsync("   ", NullLogger.Instance);
    }

    [Fact]
    public void DiscoverScripts_ShouldReturnValidListWithoutThrowing()
    {
        // Act
        var scripts = DatabaseMigrator.DiscoverScripts();

        // Assert (当前未发布阶段所有基线结构收敛在 init.sql，增量迁移脚本列表为空或包含合法脚本，不应抛出异常)
        scripts.Should().NotBeNull();
    }
}
