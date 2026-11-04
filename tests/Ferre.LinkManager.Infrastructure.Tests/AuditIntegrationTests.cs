using Ferre.LinkManager.Application;
using Ferre.LinkManager.Application.Categories.CreateCategory;
using Ferre.LinkManager.Application.Common.Abstractions;
using Ferre.LinkManager.Application.Links.CreateLink;
using Ferre.LinkManager.Application.Links.DeleteLink;
using Ferre.LinkManager.Domain.Audit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Ferre.LinkManager.Infrastructure.Tests;

public class AuditIntegrationTests : IAsyncLifetime
{
    private readonly List<Guid> _linkIds = new();
    private readonly List<Guid> _categoryIds = new();
    private readonly Guid _userId = Guid.NewGuid();

    [Fact]
    public async Task CreateAndDelete_WriteAuditLogsWithSnapshotAndUser()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddInfrastructure(TestDb.GetConnectionString());
        services.AddApplication();
        services.AddScoped<ICurrentUser>(_ => new FakeCurrentUser(_userId));

        await using var provider = services.BuildServiceProvider();
        await using var scope = provider.CreateAsyncScope();
        var sp = scope.ServiceProvider;

        var category = await sp.GetRequiredService<CreateCategoryHandler>().HandleAsync(
            new CreateCategoryCommand("Test-" + Guid.NewGuid()), CancellationToken.None);
        _categoryIds.Add(category.Id);

        var link = await sp.GetRequiredService<CreateLinkHandler>().HandleAsync(
            new CreateLinkCommand("Audit link", "https://audit.local", category.Id, new[] { "TestRole-" + Guid.NewGuid() }),
            CancellationToken.None);
        _linkIds.Add(link.Id);

        await sp.GetRequiredService<DeleteLinkHandler>().HandleAsync(
            new DeleteLinkCommand(link.Id), CancellationToken.None);

        await using var db = TestDb.CreateContext();
        var logs = await db.AuditLogs
            .Where(a => a.EntityId == link.Id || a.EntityId == category.Id)
            .ToListAsync();

        Assert.Equal(3, logs.Count);

        var categoryCreated = Assert.Single(logs, a => a.Action == AuditActions.CategoryCreated);
        Assert.Equal(category.Name, categoryCreated.EntityName);

        var linkCreated = Assert.Single(logs, a => a.Action == AuditActions.LinkCreated);
        Assert.Equal("Audit link", linkCreated.EntityName);
        Assert.Equal("https://audit.local", linkCreated.EntityUrl);

        var linkDeleted = Assert.Single(logs, a => a.Action == AuditActions.LinkDeleted);
        Assert.Equal("Audit link", linkDeleted.EntityName);
        Assert.Equal("https://audit.local", linkDeleted.EntityUrl);

        Assert.All(logs, log => Assert.Equal(_userId, log.UserId));
    }

    public Task InitializeAsync() => Task.CompletedTask;

    public Task DisposeAsync() => TestDb.CleanupAsync(_linkIds, _categoryIds);

    private sealed class FakeCurrentUser : ICurrentUser
    {
        public FakeCurrentUser(Guid userId) => UserId = userId;

        public Guid? UserId { get; }
    }
}