using System.Text.Json;
using ApiHost.JsonConverters;
using FluentAssertions;
using Identity.Application.Users.DTOs;
using Xunit;

namespace Chakra.Tests.Unit;

public class SnowflakeIdJsonConverterTests
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters =
        {
            new LongToStringJsonConverter(),
            new NullableLongToStringJsonConverter(),
        },
    };

    [Fact]
    public void CreateRoleRequest_WithLargeStringSnowflakeIds_ShouldDeserializeCorrectly()
    {
        // 超过 JS 2^53 - 1 (9007199254740991) 的 64 位雪花 ID
        const string largeId1 = "9007199254740999";
        const string largeId2 = "184628391029384756";
        var json = $$"""
            {
                "code": "Editor",
                "name": "内容编辑",
                "menuIds": ["{{largeId1}}", "{{largeId2}}"]
            }
            """;

        var result = JsonSerializer.Deserialize<CreateRoleRequest>(json, JsonOptions);

        result.Should().NotBeNull();
        result!.MenuIds.Should().NotBeNull();
        result.MenuIds.Should().HaveCount(2);
        result.MenuIds![0].ToString().Should().Be(largeId1);
        result.MenuIds![1].ToString().Should().Be(largeId2);
    }

    [Fact]
    public void CreateMenuRequest_WithLargeStringParentId_ShouldDeserializeCorrectly()
    {
        const string largeParentId = "184628391029384756";
        var json = $$"""
            {
                "name": "子菜单",
                "path": "/sub",
                "parentId": "{{largeParentId}}"
            }
            """;

        var result = JsonSerializer.Deserialize<CreateMenuRequest>(json, JsonOptions);

        result.Should().NotBeNull();
        result!.ParentId.Should().NotBeNull();
        result.ParentId!.Value.ToString().Should().Be(largeParentId);
    }
}
