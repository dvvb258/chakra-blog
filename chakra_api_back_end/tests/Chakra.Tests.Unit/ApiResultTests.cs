using FluentAssertions;
using SharedKernel.Application;
using Xunit;

namespace Chakra.Tests.Unit;

public class ApiResultTests
{
    [Fact]
    public void Success_ShouldSetCode200AndData()
    {
        var result = ApiResult<string>.Success("ok_data", "成功", "trace-123");

        result.Code.Should().Be(200);
        result.Message.Should().Be("成功");
        result.Data.Should().Be("ok_data");
        result.TraceId.Should().Be("trace-123");
    }

    [Fact]
    public void Fail_ShouldSetErrorCode()
    {
        var result = ApiResult<object?>.Fail("参数错误", 400, "trace-fail");

        result.Code.Should().Be(400);
        result.Message.Should().Be("参数错误");
        result.Data.Should().BeNull();
        result.TraceId.Should().Be("trace-fail");
    }
}
