using Content.Application.Common;
using Content.Infrastructure.Services;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace Chakra.Tests.Unit;

public class LocalStorageServiceTests
{
    private sealed class TestWebHostEnvironment : IWebHostEnvironment
    {
        public string EnvironmentName { get; set; } = "Testing";
        public string ApplicationName { get; set; } = "Chakra.Tests";
        public string WebRootPath { get; set; } = string.Empty;
        public IFileProvider WebRootFileProvider { get; set; } = new NullFileProvider();
        public string ContentRootPath { get; set; } = string.Empty;
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }

    [Fact]
    public async Task DeleteAsync_PathTraversal_ShouldBeBlocked_AndNotDeleteOutsideFile()
    {
        // Arrange
        var testRootDir = Path.Combine(Path.GetTempPath(), "chakra_test_" + Guid.NewGuid().ToString("N"));
        var uploadsDir = Path.Combine(testRootDir, "uploads");
        Directory.CreateDirectory(uploadsDir);

        // 在 uploads 目录之外创建一个敏感文件
        var sensitiveFileOutside = Path.Combine(testRootDir, "sensitive_config.json");
        await File.WriteAllTextAsync(sensitiveFileOutside, "{ \"secret\": \"do-not-delete\" }");

        var env = new TestWebHostEnvironment { ContentRootPath = testRootDir };
        var options = Options.Create(new LocalStorageSettings
        {
            UploadDir = uploadsDir,
            RequestPath = "/uploads",
        });

        var service = new LocalStorageService(
            options,
            env,
            new HttpContextAccessor(),
            NullLogger<LocalStorageService>.Instance);

        // 尝试通过目录穿越路径触发删除
        var traversalUrl = "/uploads/../sensitive_config.json";

        // Act
        await service.DeleteAsync(traversalUrl);

        // Assert: 目录外的敏感文件必须完好无损，未被删除
        File.Exists(sensitiveFileOutside).Should().BeTrue();

        // 清理临时测试目录
        try
        {
            Directory.Delete(testRootDir, true);
        }
        catch
        {
            // 忽略测试清理异常
        }
    }

    [Fact]
    public async Task DeleteAsync_ValidInsideFile_ShouldDeleteSuccessfully()
    {
        // Arrange
        var testRootDir = Path.Combine(Path.GetTempPath(), "chakra_test_" + Guid.NewGuid().ToString("N"));
        var uploadsDir = Path.Combine(testRootDir, "uploads");
        Directory.CreateDirectory(uploadsDir);

        // 在 uploads 目录内创建一个合法的图片文件
        var insideFile = Path.Combine(uploadsDir, "photo.jpg");
        await File.WriteAllTextAsync(insideFile, "fake-image-content");

        var env = new TestWebHostEnvironment { ContentRootPath = testRootDir };
        var options = Options.Create(new LocalStorageSettings
        {
            UploadDir = uploadsDir,
            RequestPath = "/uploads",
        });

        var service = new LocalStorageService(
            options,
            env,
            new HttpContextAccessor(),
            NullLogger<LocalStorageService>.Instance);

        // Act
        await service.DeleteAsync("/uploads/photo.jpg");

        // Assert: 目录内的合法文件应当被成功删除
        File.Exists(insideFile).Should().BeFalse();

        // 清理临时测试目录
        try
        {
            Directory.Delete(testRootDir, true);
        }
        catch
        {
            // 忽略测试清理异常
        }
    }
}
