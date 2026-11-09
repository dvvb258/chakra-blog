using System.Net;
using System.Text;
using Content.Application.Common;
using Content.Infrastructure.Data;
using Content.Infrastructure.Services;
using FluentAssertions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace Chakra.Tests.Unit;

public class ArticleGenerationServiceTests
{
    private sealed class FakePublisher : IPublisher
    {
        public Task Publish(object notification, CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task Publish<TNotification>(TNotification notification, CancellationToken cancellationToken = default)
            where TNotification : INotification => Task.CompletedTask;
    }

    private sealed class FakeChatClient : IChatClient
    {
        private int _callCount = 0;

        public void Dispose()
        {
        }

        public Task<ChatResponse> GetResponseAsync(
            IEnumerable<ChatMessage> chatMessages,
            ChatOptions? options = null,
            CancellationToken cancellationToken = default)
        {
            _callCount++;

            // 第一次调用：生成文章正文
            if (_callCount == 1)
            {
                var content = "# 深入理解 .NET 10 架构\n\n这是一篇自动生成的测试文章正文。\n\n> 参考原文：https://example.com/dotnet-10";
                return Task.FromResult(new ChatResponse([new ChatMessage(ChatRole.Assistant, content)]));
            }

            // 第二次调用：提取元数据 JSON
            var metaJson = """
                {
                  "title": "深入理解 .NET 10 架构",
                  "tags": [".NET", "C#"],
                  "summary": "自动生成文章摘要",
                  "categoryId": 999999,
                  "seriesId": null,
                  "coverColor": "linear-gradient(135deg, #6D28D9 0%, #818CF8 100%)"
                }
                """;
            return Task.FromResult(new ChatResponse([new ChatMessage(ChatRole.Assistant, metaJson)]));
        }

        public IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
            IEnumerable<ChatMessage> chatMessages,
            ChatOptions? options = null,
            CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public object? GetService(Type serviceType, object? serviceKey = null) => null;
    }

    private sealed class FakeHttpMessageHandler : HttpMessageHandler
    {
        private readonly string _rssXml;

        public FakeHttpMessageHandler(string rssXml)
        {
            _rssXml = rssXml;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(_rssXml, Encoding.UTF8, "application/rss+xml"),
            };
            return Task.FromResult(response);
        }
    }

    private sealed class FakeHttpClientFactory : IHttpClientFactory
    {
        private readonly FakeHttpMessageHandler _handler;

        public FakeHttpClientFactory(string rssXml)
        {
            _handler = new FakeHttpMessageHandler(rssXml);
        }

        public HttpClient CreateClient(string name) => new(_handler);
    }

    private static BlogDbContext CreateInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<BlogDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        return new BlogDbContext(options, new FakePublisher());
    }

    [Fact]
    public async Task GenerateArticleAsync_WhenNoCategoriesExist_ShouldAutoCreateDefaultCategory_AndSucceed()
    {
        // Arrange
        await using var db = CreateInMemoryDbContext();

        // 数据库初始没有任何分类
        (await db.Categories.CountAsync()).Should().Be(0);

        var rssXml = $"""
            <?xml version="1.0" encoding="utf-8"?>
            <rss version="2.0">
              <channel>
                <title>.NET Blog</title>
                <link>https://devblogs.microsoft.com/dotnet</link>
                <item>
                  <title>.NET 10 Announcement</title>
                  <link>https://example.com/dotnet-10-{Guid.NewGuid():N}</link>
                  <pubDate>{DateTime.UtcNow.ToString("R")}</pubDate>
                  <description>New features in .NET 10</description>
                </item>
              </channel>
            </rss>
            """;

        var clientFactory = new FakeHttpClientFactory(rssXml);
        var chatClient = new FakeChatClient();
        var aiOptions = Options.Create(new AISettings
        {
            AutoGenerateEnabled = true,
            Key = "test-key",
            Endpoint = "https://api.openai.com/v1",
            ModelName = "gpt-4o-mini",
            RssSources = ["https://devblogs.microsoft.com/dotnet/feed/"],
        });

        var service = new ArticleGenerationService(
            db,
            clientFactory,
            chatClient,
            aiOptions,
            NullLogger<ArticleGenerationService>.Instance);

        // Act
        var result = await service.GenerateArticleAsync(CancellationToken.None);

        // Assert
        result.Should().BeTrue();

        // 验证系统自动创建了默认分类，且文章关联到了真实的分类 ID，而非硬编码的 1
        var categories = await db.Categories.ToListAsync();
        categories.Should().HaveCount(1);
        categories[0].Name.Should().Be("技术分享");

        var article = await db.Articles.FirstOrDefaultAsync();
        article.Should().NotBeNull();
        article!.CategoryId.Should().Be(categories[0].Id);
        article.Status.Should().Be(Content.Domain.Enums.ArticleStatus.Published);
    }
}
