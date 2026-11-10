using Ferre.LinkManager.Application;
using Ferre.LinkManager.Application.Audit.GetAuditLogs;
using Ferre.LinkManager.Application.Common.Abstractions;
using Ferre.LinkManager.Domain.Audit;
using Ferre.LinkManager.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Ferre.LinkManager.Infrastructure.Tests;

public class AuditLogQueryIntegrationTests : IAsyncLifetime
{
    private readonly string _email = $"test-{Guid.NewGuid()}@example.test";
    private readonly List<Guid> _entityIds = new();

    // A random day in the 1990s: the real audit log never has records there.
    private readonly DateTime _day =
        new DateTime(1990, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddDays(Random.Shared.Next(0, 3000));

    private Guid _userId;
    private ServiceProvider? _provider;

    private async Task<GetAuditLogsHandler> SeedAsync()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddInfrastructure(TestDb.GetConnectionString());
        services.AddApplication();
        services.AddSingleton<ICurrentUser, NullCurrentUser>();
        _provider = services.BuildServiceProvider();

        var scope = _provider.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = _email,
            Email = _email,
            EmailConfirmed = true,
            IsActive = true
        };

        var created = await userManager.CreateAsync(user);
        Assert.True(created.Succeeded);
        _userId = user.Id;

        var linkId1 = Guid.NewGuid();
        var linkId2 = Guid.NewGuid();
        var linkId3 = Guid.NewGuid();
        _entityIds.AddRange(new[] { _userId, linkId1, linkId2, linkId3 });

        await using var db = TestDb.CreateContext();
        db.AuditLogs.AddRange(
            AuditLog.Create(AuditActions.LinkCreated, "Link", linkId1, "First", "https://one.example", _userId, _day.AddHours(1)),
            AuditLog.Create(AuditActions.LinkDeleted, "Link", linkId2, "Second", "https://two.example", _userId, _day.AddHours(2)),
            AuditLog.Create(AuditActions.LinkCreated, "Link", linkId3, "Third", "https://three.example", _userId, _day.AddHours(3)),
            AuditLog.Create(AuditActions.UserDeactivated, "User", _userId, null, null, _userId, _day.AddHours(4)));
        await db.SaveChangesAsync();

        return scope.ServiceProvider.GetRequiredService<GetAuditLogsHandler>();
    }

    private GetAuditLogsQuery Query(string? action = null, int page = 1, int pageSize = 20) =>
        new(page, pageSize, action, _day, _day.AddDays(1));

    [Fact]
    public async Task FiltersByAction_AndSortsNewestFirst()
    {
        var handler = await SeedAsync();

        var result = await handler.HandleAsync(Query(AuditActions.LinkCreated), CancellationToken.None);

        Assert.Equal(2, result.TotalCount);
        Assert.Equal(new[] { "Third", "First" }, result.Items.Select(item => item.EntityName).ToArray());
    }

    [Fact]
    public async Task Pages_TheResults()
    {
        var handler = await SeedAsync();

        var first = await handler.HandleAsync(Query(pageSize: 3), CancellationToken.None);
        var second = await handler.HandleAsync(Query(page: 2, pageSize: 3), CancellationToken.None);

        Assert.Equal(4, first.TotalCount);
        Assert.Equal(3, first.Items.Count);
        Assert.Single(second.Items);
        Assert.Equal(2, second.Page);
    }

    [Fact]
    public async Task ResolvesEmails_ForTheActorAndForTheTargetUser()
    {
        var handler = await SeedAsync();

        var result = await handler.HandleAsync(Query(AuditActions.UserDeactivated), CancellationToken.None);

        var item = Assert.Single(result.Items);
        Assert.Equal(_email, item.UserEmail);
        Assert.Equal(_email, item.EntityName);
    }

    [Fact]
    public async Task MarksTheTimeAsUtc_SoTheJsonEndsWithZ()
    {
        var handler = await SeedAsync();

        var result = await handler.HandleAsync(Query(), CancellationToken.None);

        Assert.All(result.Items, item => Assert.Equal(DateTimeKind.Utc, item.OccurredOnUtc.Kind));
    }

    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync()
    {
        await using var db = TestDb.CreateContext();

        var entityIds = _entityIds.ToArray();
        var userId = _userId;

        await db.AuditLogs
            .Where(log => entityIds.Contains(log.EntityId) || (log.UserId.HasValue && log.UserId == userId))
            .ExecuteDeleteAsync();

        await db.Users.Where(user => user.Email == _email).ExecuteDeleteAsync();

        if (_provider is not null)
        {
            await _provider.DisposeAsync();
        }
    }

    private sealed class NullCurrentUser : ICurrentUser
    {
        public Guid? UserId => null;
    }
}