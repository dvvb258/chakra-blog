using Content.Domain.Entities;
using FluentAssertions;
using Xunit;

namespace Chakra.Tests.Unit;

public class ArticleSummaryTests
{
    [Fact]
    public void ExtractSummary_WithCodeBlockAndHtml_ShouldStripNoiseCleanly()
    {
        // Arrange
        var markdown = """
            # 深入剖析 .NET 10 新特性

            这是一段引言介绍，用于快速了解全文。

            ```csharp
            public class Program
            {
                public static void Main() => Console.WriteLine("Hello World");
            }
            ```

            在本文中，我们将探讨 `<span style="color:red">核心模块</span>` 的优化细节。
            更多详情请见 [官方文档](https://learn.microsoft.com)。
            """;

        // Act
        var summary = Article.ExtractSummary(markdown);

        // Assert
        summary.Should().NotContain("public class Program");
        summary.Should().NotContain("```");
        summary.Should().NotContain("<span");
        summary.Should().NotContain("https://learn.microsoft.com");
        summary.Should().Contain("深入剖析 .NET 10 新特性");
        summary.Should().Contain("这是一段引言介绍");
        summary.Should().Contain("官方文档");
    }

    [Fact]
    public void ExtractSummary_EmptyOrWhitespace_ShouldReturnEmpty()
    {
        Article.ExtractSummary("").Should().BeEmpty();
        Article.ExtractSummary("   ").Should().BeEmpty();
    }
}
