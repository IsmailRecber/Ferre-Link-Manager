using Ferre.LinkManager.Application;
using Ferre.LinkManager.Application.Categories.CreateCategory;
using Ferre.LinkManager.Application.Categories.UpdateCategory;
using Ferre.LinkManager.Application.Common.Abstractions;
using Ferre.LinkManager.Application.Links.CreateLink;
using Ferre.LinkManager.Application.Links.SetLinkActive;
using Ferre.LinkManager.Domain.Audit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Ferre.LinkManager.Infrastructure.Tests;

public class StatusChangeAuditIntegrationTests : IAsyncLifetime
{
    private readonly List<Guid> _linkIds = new();
    private readonly List<Guid> _categoryIds = new();
    private readonly Guid _adminId = Guid.NewGuid();

    [Fact]
    public async Task RenameActivateAndDeactivate_AreRecorded_AndUnchangedCallsAreNot()
    {
        var originalName = "Test-" + Guid.NewGuid();
        var newName = "Test-" + Guid.NewGuid();

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddInfrastructure(TestDb.GetConnectionString());
        services.AddApplication();
        services.AddSingleton<ICurrentUser>(new TestCurrentUser(_adminId));

        await using var provider = services.BuildServiceProvider();

        // Every step runs in its own scope, like separate HTTP requests.
        var category = await InScope(provider, sp => sp.GetRequiredService<CreateCategoryHandler>().HandleAsync(
            new CreateCategoryCommand(originalName), CancellationToken.None));
        _categoryIds.Add(category.Id);

        var link = await InScope(provider, sp => sp.GetRequiredService<CreateLinkHandler>().HandleAsync(
            new CreateLinkCommand("Audit link", "https://audit.example", category.Id, new[] { "User" }),
            CancellationToken.None));
        _linkIds.Add(link.Id);

        // Category: rename + deactivate in one call (two records).
        await InScope(provider, sp => sp.GetRequiredService<UpdateCategoryHandler>().HandleAsync(
            new UpdateCategoryCommand(category.Id, newName, false), CancellationToken.None));

        // The same call again changes nothing, so it must add no record.
        await InScope(provider, sp => sp.GetRequiredService<UpdateCategoryHandler>().HandleAsync(
            new UpdateCategoryCommand(category.Id, newName, false), CancellationToken.None));

        // Category: activate again (one record).
        await InScope(provider, sp => sp.GetRequiredService<UpdateCategoryHandler>().HandleAsync(
            new UpdateCategoryCommand(category.Id, newName, true), CancellationToken.None));

        // Link: deactivate, deactivate again (no record), activate.
        await InScope(provider, sp => sp.GetRequiredService<SetLinkActiveHandler>().HandleAsync(
            new SetLinkActiveCommand(link.Id, false), CancellationToken.None));
        await InScope(provider, sp => sp.GetRequiredService<SetLinkActiveHandler>().HandleAsync(
            new SetLinkActiveCommand(link.Id, false), CancellationToken.None));
        await InScope(provider, sp => sp.GetRequiredService<SetLinkActiveHandler>().HandleAsync(
            new SetLinkActiveCommand(link.Id, true), CancellationToken.None));

        await using var db = TestDb.CreateContext();

        var categoryActions = await db.AuditLogs
            .Where(log => log.EntityId == category.Id)
            .Select(log => log.Action)
            .ToListAsync();

        var linkActions = await db.AuditLogs
            .Where(log => log.EntityId == link.Id)
            .Select(log => log.Action)
            .ToListAsync();

        Assert.Equal(
            new[]
            {
                AuditActions.CategoryActivated,
                AuditActions.CategoryCreated,
                AuditActions.CategoryDeactivated,
                AuditActions.CategoryRenamed
            },
            categoryActions.OrderBy(action => action, StringComparer.Ordinal).ToArray());

        Assert.Equal(
            new[] { AuditActions.LinkActivated, AuditActions.LinkCreated, AuditActions.LinkDeactivated },
            linkActions.OrderBy(action => action, StringComparer.Ordinal).ToArray());

        var renamed = await db.AuditLogs.SingleAsync(
            log => log.EntityId == category.Id && log.Action == AuditActions.CategoryRenamed);

        Assert.Equal($"{originalName} → {newName}", renamed.EntityName);
        Assert.Equal(_adminId, renamed.UserId);
    }

    private static async Task<T> InScope<T>(ServiceProvider provider, Func<IServiceProvider, Task<T>> action)
    {
        await using var scope = provider.CreateAsyncScope();

        return await action(scope.ServiceProvider);
    }

    private static async Task InScope(ServiceProvider provider, Func<IServiceProvider, Task> action)
    {
        await using var scope = provider.CreateAsyncScope();

        await action(scope.ServiceProvider);
    }

    public Task InitializeAsync() => Task.CompletedTask;

    public Task DisposeAsync() => TestDb.CleanupAsync(_linkIds, _categoryIds);

    private sealed class TestCurrentUser : ICurrentUser
    {
        public TestCurrentUser(Guid userId) => UserId = userId;

        public Guid? UserId { get; }
    }
}