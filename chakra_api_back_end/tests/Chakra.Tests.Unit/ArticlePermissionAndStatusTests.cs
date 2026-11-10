using Content.Application.Articles.Commands;
using Content.Application.Articles.DTOs;
using Content.Application.Stats.Commands;
using Content.Domain.Entities;
using Content.Domain.Enums;
using Content.Infrastructure.Data;
using FluentAssertions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using SharedKernel.Application.Exceptions;
using Xunit;

namespace Chakra.Tests.Unit;

public class ArticlePermissionAndStatusTests
{
    private sealed class FakePublisher : IPublisher
    {
        public Task Publish(object notification, CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task Publish<TNotification>(TNotification notification, CancellationToken cancellationToken = default)
            where TNotification : INotification => Task.CompletedTask;
    }

    private static BlogDbContext CreateInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<BlogDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        return new BlogDbContext(options, new FakePublisher());
    }

    [Fact]
    public async Task Admin_ShouldBeAbleToUpdate_AiGeneratedArticle()
    {
        // Arrange
        await using var db = CreateInMemoryDbContext();
        var category = new Category("技术分享");
        db.Categories.Add(category);

        // AI 生成的文章 CreatedByUserId 为 0
        var aiArticle = new Article(
            title: "AI 生成的文章",
            content: "正文内容",
            summary: "摘要",
            tags: ["AI"],
            categoryId: category.Id,
            author: "AI生成",
            readingTime: 3,
            coverColor: "#0284c7",
            sourceLink: "https://example.com/source",
            createdByUserId: 0,
            status: ArticleStatus.Published);
        db.Articles.Add(aiArticle);
        await db.SaveChangesAsync();

        var handler = new UpdateArticleCommandHandler(db);
        var updateRequest = new UpdateArticleRequest
        {
            Title = "管理员修改后的标题",
            Content = "修改后的正文内容",
            CategoryId = category.Id,
            Status = (int)ArticleStatus.Published,
            Version = aiArticle.Version,
        };

        // 当前登录人是管理员 (Id = 1, IsAdmin = true)
        var command = new UpdateArticleCommand(
            aiArticle.Id,
            updateRequest,
            CurrentUserId: 1,
            IsAdmin: true);

        // Act
        await handler.Handle(command, CancellationToken.None);

        // Assert
        var updated = await db.Articles.FindAsync(aiArticle.Id);
        updated.Should().NotBeNull();
        updated!.Title.Should().Be("管理员修改后的标题");
        updated.Status.Should().Be(ArticleStatus.Published);
    }

    [Fact]
    public async Task Admin_ShouldBeAbleToUpdate_AnotherUserArticle_AndKeepPublished()
    {
        // Arrange
        await using var db = CreateInMemoryDbContext();
        var category = new Category("技术分享");
        db.Categories.Add(category);

        // 由普通用户 999 创建的已发布文章
        var userArticle = new Article(
            title: "用户999的文章",
            content: "正文内容",
            summary: "摘要",
            tags: [".NET"],
            categoryId: category.Id,
            author: "用户999",
            readingTime: 3,
            coverColor: "#0284c7",
            createdByUserId: 999,
            status: ArticleStatus.Published);
        db.Articles.Add(userArticle);
        await db.SaveChangesAsync();

        var handler = new UpdateArticleCommandHandler(db);
        var updateRequest = new UpdateArticleRequest
        {
            Title = "管理员润色后的标题",
            Content = "正文内容修复错别字",
            CategoryId = category.Id,
            Status = (int)ArticleStatus.Published,
            Version = userArticle.Version,
        };

        // 当前登录人是管理员 (Id = 1, IsAdmin = true)
        var command = new UpdateArticleCommand(
            userArticle.Id,
            updateRequest,
            CurrentUserId: 1,
            IsAdmin: true);

        // Act
        await handler.Handle(command, CancellationToken.None);

        // Assert
        var updated = await db.Articles.FindAsync(userArticle.Id);
        updated.Should().NotBeNull();
        updated!.Title.Should().Be("管理员润色后的标题");

        // 关键断言：管理员修改后不应被降级为草稿，应保持已发布
        updated.Status.Should().Be(ArticleStatus.Published);
    }

    [Fact]
    public async Task NonAdmin_UpdatingAnotherUserArticle_ShouldThrowForbidden()
    {
        // Arrange
        await using var db = CreateInMemoryDbContext();
        var category = new Category("技术分享");
        db.Categories.Add(category);

        var originalArticle = new Article(
            title: "原作者文章",
            content: "正文内容",
            summary: "摘要",
            tags: ["C#"],
            categoryId: category.Id,
            author: "原作者",
            readingTime: 3,
            coverColor: "#0284c7",
            createdByUserId: 100,
            status: ArticleStatus.Published);
        db.Articles.Add(originalArticle);
        await db.SaveChangesAsync();

        var handler = new UpdateArticleCommandHandler(db);
        var updateRequest = new UpdateArticleRequest
        {
            Title = "试图篡改标题",
            Content = "试图篡改正文",
            CategoryId = category.Id,
            Status = (int)ArticleStatus.Draft,
            Version = originalArticle.Version,
        };

        // 当前操作人是另一个普通用户 (Id = 200, IsAdmin = false)
        var command = new UpdateArticleCommand(
            originalArticle.Id,
            updateRequest,
            CurrentUserId: 200,
            IsAdmin: false);

        // Act & Assert
        var act = async () => await handler.Handle(command, CancellationToken.None);
        await act.Should().ThrowAsync<BusinessException>()
            .WithMessage("*权限不足*");
    }

    [Fact]
    public async Task Admin_CreatingArticleWithPublishedStatus_ShouldPublishDirectly()
    {
        // Arrange
        await using var db = CreateInMemoryDbContext();
        var category = new Category("技术分享");
        db.Categories.Add(category);
        await db.SaveChangesAsync();

        var handler = new CreateArticleCommandHandler(db);
        var createRequest = new CreateArticleRequest
        {
            Title = "管理员新发文章",
            Content = "正文内容十分精彩",
            CategoryId = category.Id,
            Status = (int)ArticleStatus.Published,
        };

        var command = new CreateArticleCommand(
            createRequest,
            CurrentUserId: 1,
            CurrentUserNickname: "超级管理员",
            IsAdmin: true);

        // Act
        var articleId = await handler.Handle(command, CancellationToken.None);

        // Assert
        var created = await db.Articles.FindAsync(articleId);
        created.Should().NotBeNull();
        created!.Status.Should().Be(ArticleStatus.Published);
    }

    [Fact]
    public async Task NonAdmin_CreatingArticleWithPublishedStatus_ShouldFallBackToDraft()
    {
        // Arrange
        await using var db = CreateInMemoryDbContext();
        var category = new Category("技术分享");
        db.Categories.Add(category);
        await db.SaveChangesAsync();

        var handler = new CreateArticleCommandHandler(db);
        var createRequest = new CreateArticleRequest
        {
            Title = "普通作者新发文章",
            Content = "正文内容",
            CategoryId = category.Id,
            Status = (int)ArticleStatus.Published, // 试图直接发布
        };

        var command = new CreateArticleCommand(
            createRequest,
            CurrentUserId: 200,
            CurrentUserNickname: "普通作者",
            IsAdmin: false);

        // Act
        var articleId = await handler.Handle(command, CancellationToken.None);

        // Assert
        var created = await db.Articles.FindAsync(articleId);
        created.Should().NotBeNull();

        // 普通用户无权直接发布，必须降为草稿或提审
        created!.Status.Should().Be(ArticleStatus.Draft);
    }

    [Fact]
    public async Task Admin_ShouldBeAbleToCreatePinnedArticle_AndTogglePin()
    {
        // Arrange
        await using var db = CreateInMemoryDbContext();
        var category = new Category("技术分享");
        db.Categories.Add(category);
        await db.SaveChangesAsync();

        var createHandler = new CreateArticleCommandHandler(db);
        var createRequest = new CreateArticleRequest
        {
            Title = "管理员置顶文章",
            Content = "正文内容",
            CategoryId = category.Id,
            Status = (int)ArticleStatus.Published,
            IsPinned = true,
        };

        var createCommand = new CreateArticleCommand(
            createRequest,
            CurrentUserId: 1,
            CurrentUserNickname: "管理员",
            IsAdmin: true);

        // Act - Create with IsPinned
        var articleId = await createHandler.Handle(createCommand, CancellationToken.None);

        // Assert created
        var article = await db.Articles.FindAsync(articleId);
        article.Should().NotBeNull();
        article!.IsPinned.Should().BeTrue();

        // Act - Toggle pin to false
        var toggleHandler = new ToggleArticlePinCommandHandler(db);
        await toggleHandler.Handle(new ToggleArticlePinCommand(articleId, false), CancellationToken.None);

        // Assert toggled
        var updated = await db.Articles.FindAsync(articleId);
        updated!.IsPinned.Should().BeFalse();
    }

    [Fact]
    public async Task NonAdmin_CannotSetPinned_OnArticleCreation()
    {
        // Arrange
        await using var db = CreateInMemoryDbContext();
        var category = new Category("技术分享");
        db.Categories.Add(category);
        await db.SaveChangesAsync();

        var createHandler = new CreateArticleCommandHandler(db);
        var createRequest = new CreateArticleRequest
        {
            Title = "普通用户尝试置顶文章",
            Content = "正文内容",
            CategoryId = category.Id,
            IsPinned = true, // 试图置顶
        };

        var createCommand = new CreateArticleCommand(
            createRequest,
            CurrentUserId: 100,
            CurrentUserNickname: "普通作者",
            IsAdmin: false);

        // Act
        var articleId = await createHandler.Handle(createCommand, CancellationToken.None);

        // Assert - 非管理员不可置顶
        var article = await db.Articles.FindAsync(articleId);
        article.Should().NotBeNull();
        article!.IsPinned.Should().BeFalse();
    }

    [Fact]
    public async Task IncrementArticleLike_ShouldIncreaseLikes_AndPreventDuplicateByCache()
    {
        // Arrange
        await using var db = CreateInMemoryDbContext();
        using var cache = new MemoryCache(new MemoryCacheOptions());
        var category = new Category("技术分享");
        db.Categories.Add(category);

        var article = new Article(
            title: "测试点赞文章",
            content: "正文内容",
            summary: "摘要",
            tags: ["DotNet"],
            categoryId: category.Id,
            author: "测试作者",
            readingTime: 2,
            coverColor: "#0284c7");
        db.Articles.Add(article);
        await db.SaveChangesAsync();

        var handler = new IncrementArticleLikeCommandHandler(db, cache);

        // Act 1: 第一次点赞
        await handler.Handle(new IncrementArticleLikeCommand(article.Id, ClientIp: "127.0.0.1", VisitorId: "vid_1"), CancellationToken.None);

        // Assert 1: Likes 变为 1
        var updated = await db.Articles.FindAsync(article.Id);
        updated!.Likes.Should().Be(1);

        // Act 2: 同一访客在防刷有效期内重复调用点赞
        await handler.Handle(new IncrementArticleLikeCommand(article.Id, ClientIp: "127.0.0.1", VisitorId: "vid_1"), CancellationToken.None);

        // Assert 2: 触发幂等防刷，Likes 依然为 1
        updated = await db.Articles.FindAsync(article.Id);
        updated!.Likes.Should().Be(1);

        // Act 3: 另一个访客点赞
        await handler.Handle(new IncrementArticleLikeCommand(article.Id, ClientIp: "127.0.0.2", VisitorId: "vid_2"), CancellationToken.None);

        // Assert 3: Likes 变为 2
        updated = await db.Articles.FindAsync(article.Id);
        updated!.Likes.Should().Be(2);
    }

    [Fact]
    public async Task DecrementArticleLike_ShouldDecreaseLikes_AndPreventNegative()
    {
        // Arrange
        await using var db = CreateInMemoryDbContext();
        using var cache = new MemoryCache(new MemoryCacheOptions());
        var category = new Category("技术分享");
        db.Categories.Add(category);

        var article = new Article(
            title: "测试取消点赞文章",
            content: "正文内容",
            summary: "摘要",
            tags: ["DotNet"],
            categoryId: category.Id,
            author: "测试作者",
            readingTime: 2,
            coverColor: "#0284c7");
        db.Articles.Add(article);
        await db.SaveChangesAsync();

        var incHandler = new IncrementArticleLikeCommandHandler(db, cache);
        var decHandler = new DecrementArticleLikeCommandHandler(db, cache);

        // 访客点赞
        await incHandler.Handle(new IncrementArticleLikeCommand(article.Id, ClientIp: "127.0.0.1", VisitorId: "vid_1"), CancellationToken.None);
        var updated = await db.Articles.FindAsync(article.Id);
        updated!.Likes.Should().Be(1);

        // Act 1: 访客取消点赞
        await decHandler.Handle(new DecrementArticleLikeCommand(article.Id, ClientIp: "127.0.0.1", VisitorId: "vid_1"), CancellationToken.None);

        // Assert 1: Likes 回退为 0
        updated = await db.Articles.FindAsync(article.Id);
        updated!.Likes.Should().Be(0);

        // Act 2: 再次取消点赞（或未点过赞恶意调取消）
        await decHandler.Handle(new DecrementArticleLikeCommand(article.Id, ClientIp: "127.0.0.1", VisitorId: "vid_1"), CancellationToken.None);

        // Assert 2: 保底非负，Likes 依然为 0
        updated = await db.Articles.FindAsync(article.Id);
        updated!.Likes.Should().Be(0);
    }
}
