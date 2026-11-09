using Content.Application.Settings.Commands;
using Content.Infrastructure.Data;
using FluentAssertions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Chakra.Tests.Unit;

public class SiteSettingsTests
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
    public void Validator_ShouldAccept_JsonObject()
    {
        var validator = new UpdateSiteSettingsCommandValidator();
        var command = new UpdateSiteSettingsCommand("""{"name":"我的博客","socials":{"github":"https://github.com/x"}}""");

        validator.Validate(command).IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validator_ShouldReject_InvalidJson()
    {
        var validator = new UpdateSiteSettingsCommandValidator();

        validator.Validate(new UpdateSiteSettingsCommand("{name:}")).IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validator_ShouldReject_NonObjectRoot()
    {
        var validator = new UpdateSiteSettingsCommandValidator();

        validator.Validate(new UpdateSiteSettingsCommand("[1,2,3]")).IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validator_ShouldReject_ForbiddenKeys()
    {
        var validator = new UpdateSiteSettingsCommandValidator();

        validator.Validate(new UpdateSiteSettingsCommand("""{"__proto__":{"polluted":true}}""")).IsValid.Should().BeFalse();
        validator.Validate(new UpdateSiteSettingsCommand("""{"about":{"constructor":"x"}}""")).IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validator_ShouldReject_InvalidSiteUrl()
    {
        var validator = new UpdateSiteSettingsCommandValidator();

        validator.Validate(new UpdateSiteSettingsCommand("""{"url":"not-a-url"}""")).IsValid.Should().BeFalse();
        validator.Validate(new UpdateSiteSettingsCommand("""{"url":"javascript:alert(1)"}""")).IsValid.Should().BeFalse();
        validator.Validate(new UpdateSiteSettingsCommand("""{"url":"https://blog.example.com"}""")).IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validator_ShouldReject_NullConfig()
    {
        var validator = new UpdateSiteSettingsCommandValidator();

        validator.Validate(new UpdateSiteSettingsCommand("null")).IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validator_ShouldReject_OversizedConfig()
    {
        var validator = new UpdateSiteSettingsCommandValidator();
        var payload = "{\"description\":\"" + new string('a', 70 * 1024) + "\"}";

        validator.Validate(new UpdateSiteSettingsCommand(payload)).IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task Handler_ShouldCreateSingleton_ThenUpdateInPlace()
    {
        await using var db = CreateInMemoryDbContext();
        var handler = new UpdateSiteSettingsCommandHandler(db);

        await handler.Handle(new UpdateSiteSettingsCommand("""{"name":"第一版"}"""), CancellationToken.None);
        await handler.Handle(new UpdateSiteSettingsCommand("""{"name":"第二版"}"""), CancellationToken.None);

        var rows = await db.SiteSettings.ToListAsync();
        rows.Should().HaveCount(1);
        rows[0].Id.Should().Be(1);
        rows[0].ConfigJson.Should().Be("""{"name":"第二版"}""");
    }
}
