using Ferre.LinkManager.Application;
using Ferre.LinkManager.Application.Common.Abstractions;
using Ferre.LinkManager.Application.Users.GetUsers;
using Ferre.LinkManager.Application.Users.Register;
using Ferre.LinkManager.Application.Users.SetUserActive;
using Ferre.LinkManager.Domain.Audit;
using Ferre.LinkManager.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Ferre.LinkManager.Infrastructure.Tests;

public class UserManagementIntegrationTests : IAsyncLifetime
{
    private readonly string _email = $"test-{Guid.NewGuid()}@example.test";
    private readonly Guid _adminId = Guid.NewGuid();
    private Guid _userId;

    [Fact]
    public async Task Deactivate_DisablesUserInvalidatesSessionsAndWritesAudit()
    {
        await using var provider = BuildProvider();
        string stampBefore;

        await using (var scope = provider.CreateAsyncScope())
        {
            var response = await scope.ServiceProvider.GetRequiredService<RegisterHandler>().HandleAsync(
                new RegisterCommand(_email, "Abcdef1"), CancellationToken.None);
            _userId = response.UserId;

            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var user = await userManager.FindByIdAsync(_userId.ToString());
            stampBefore = await userManager.GetSecurityStampAsync(user!);
        }

        await using (var scope = provider.CreateAsyncScope())
        {
            await scope.ServiceProvider.GetRequiredService<SetUserActiveHandler>().HandleAsync(
                new SetUserActiveCommand(_userId, false), CancellationToken.None);
        }

        await using (var scope = provider.CreateAsyncScope())
        {
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var user = await userManager.FindByIdAsync(_userId.ToString());

            Assert.False(user!.IsActive);
            Assert.NotEqual(stampBefore, await userManager.GetSecurityStampAsync(user));
        }

        await using var db = TestDb.CreateContext();
        Assert.True(await db.AuditLogs.AnyAsync(a =>
            a.EntityId == _userId && a.Action == AuditActions.UserDeactivated && a.UserId == _adminId));
    }

    [Fact]
    public async Task GetUsers_IncludesTheUserWithItsRole()
    {
        await using var provider = BuildProvider();
        await using var scope = provider.CreateAsyncScope();
        var sp = scope.ServiceProvider;

        var response = await sp.GetRequiredService<RegisterHandler>().HandleAsync(
            new RegisterCommand(_email, "Abcdef1"), CancellationToken.None);
        _userId = response.UserId;

        var users = await sp.GetRequiredService<GetUsersHandler>().HandleAsync(CancellationToken.None);

        var ours = Assert.Single(users, user => user.Id == _userId);
        Assert.Equal(_email, ours.Email);
        Assert.True(ours.IsActive);
        Assert.False(ours.EmailConfirmed);
        Assert.Equal("User", Assert.Single(ours.Roles));
    }

    private ServiceProvider BuildProvider()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddInfrastructure(TestDb.GetConnectionString());
        services.AddApplication();
        services.AddSingleton<ICurrentUser>(new TestCurrentUser(_adminId));

        // Registered last, so it replaces the real sender: tests never send real e-mails.
        services.AddSingleton<IEmailSender, NoOpEmailSender>();

        return services.BuildServiceProvider();
    }

    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync()
    {
        await using var db = TestDb.CreateContext();

        if (_userId != Guid.Empty)
        {
            await db.AuditLogs.Where(a => a.EntityId == _userId).ExecuteDeleteAsync();
        }

        await db.Users.Where(u => u.Email == _email).ExecuteDeleteAsync();
    }

    private sealed class TestCurrentUser : ICurrentUser
    {
        public TestCurrentUser(Guid userId) => UserId = userId;

        public Guid? UserId { get; }
    }

    private sealed class NoOpEmailSender : IEmailSender
    {
        public Task SendAsync(string to, string subject, string body, CancellationToken cancellationToken) =>
            Task.CompletedTask;
    }
}