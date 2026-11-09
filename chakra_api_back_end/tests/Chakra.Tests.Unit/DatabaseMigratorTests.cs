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
    public void EmbeddedMigrationResources_ShouldContainSiteSettingsMigration()
    {
        // Arrange
        var assembly = typeof(DatabaseMigrator).Assembly;
        var resourceNames = assembly.GetManifestResourceNames();

        // Assert
        resourceNames.Should().Contain(name => name.EndsWith("001_site_settings.sql", StringComparison.OrdinalIgnoreCase));

        var targetResource = resourceNames.First(name => name.EndsWith("001_site_settings.sql", StringComparison.OrdinalIgnoreCase));
        using var stream = assembly.GetManifestResourceStream(targetResource);
        stream.Should().NotBeNull();

        using var reader = new StreamReader(stream!);
        var sql = reader.ReadToEnd();
        sql.Should().Contain("CREATE TABLE IF NOT EXISTS site_settings");
        sql.Should().Contain("settings:manage");
    }
}
