using Ferre.LinkManager.Application;
using Ferre.LinkManager.Application.Common.Abstractions;
using Ferre.LinkManager.Application.Common.Authorization;
using Ferre.LinkManager.Application.Common.Exceptions;
using Ferre.LinkManager.Application.Users.ConfirmEmail;
using Ferre.LinkManager.Application.Users.Register;
using Ferre.LinkManager.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Ferre.LinkManager.Infrastructure.Tests;

public class RegistrationIntegrationTests : IAsyncLifetime
{
    private readonly string _email = $"test-{Guid.NewGuid()}@example.test";
    private readonly CapturingEmailSender _emailSender = new();

    [Fact]
    public async Task Register_ThenConfirmWithEmailedLink_ConfirmsUserWithUserRole()
    {
        await using var provider = BuildProvider();
        await using var scope = provider.CreateAsyncScope();
        var sp = scope.ServiceProvider;

        var response = await sp.GetRequiredService<RegisterHandler>().HandleAsync(
            new RegisterCommand(_email, "Abcdef1"), CancellationToken.None);

        var mail = Assert.Single(_emailSender.Sent);
        Assert.Equal(_email, mail.To);

        var query = new Uri(ExtractLink(mail.Body)).Query;
        var token = Uri.UnescapeDataString(query.Split("token=")[1]);

        var userManager = sp.GetRequiredService<UserManager<ApplicationUser>>();
        var before = await userManager.FindByIdAsync(response.UserId.ToString());
        Assert.False(before!.EmailConfirmed);
        Assert.True(await userManager.IsInRoleAsync(before, Roles.User));
        Assert.False(await userManager.IsInRoleAsync(before, Roles.Admin));

        await sp.GetRequiredService<ConfirmEmailHandler>().HandleAsync(
            new ConfirmEmailCommand(response.UserId, token), CancellationToken.None);

        await using var verifyScope = provider.CreateAsyncScope();
        var after = await verifyScope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>()
            .FindByIdAsync(response.UserId.ToString());
        Assert.True(after!.EmailConfirmed);
    }

    [Fact]
    public async Task Confirm_WithWrongToken_ThrowsInvalidToken()
    {
        await using var provider = BuildProvider();
        await using var scope = provider.CreateAsyncScope();
        var sp = scope.ServiceProvider;

        var response = await sp.GetRequiredService<RegisterHandler>().HandleAsync(
            new RegisterCommand(_email, "Abcdef1"), CancellationToken.None);

        var exception = await Assert.ThrowsAsync<ValidationException>(() =>
            sp.GetRequiredService<ConfirmEmailHandler>().HandleAsync(
                new ConfirmEmailCommand(response.UserId, "bm90LWEtcmVhbC10b2tlbg"), CancellationToken.None));

        Assert.Contains("InvalidToken", exception.Errors);
    }

    [Fact]
    public async Task Register_WithWeakPassword_ThrowsValidationAndCreatesNoUser()
    {
        await using var provider = BuildProvider();
        await using var scope = provider.CreateAsyncScope();

        var exception = await Assert.ThrowsAsync<ValidationException>(() =>
            scope.ServiceProvider.GetRequiredService<RegisterHandler>().HandleAsync(
                new RegisterCommand(_email, "abc"), CancellationToken.None));

        Assert.NotEmpty(exception.Errors);

        await using var db = TestDb.CreateContext();
        Assert.False(await db.Users.AnyAsync(u => u.Email == _email));
    }

    private ServiceProvider BuildProvider()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddInfrastructure(
            TestDb.GetConnectionString(),
            configureFrontend: options => options.BaseUrl = "https://app.test");
        services.AddApplication();
        services.AddSingleton<ICurrentUser, NullCurrentUser>();

        // Registered last, so it replaces the real sender: tests never send real e-mails.
        services.AddSingleton<IEmailSender>(_emailSender);

        return services.BuildServiceProvider();
    }

    private static string ExtractLink(string body) =>
        body.Split(' ').Single(part => part.StartsWith("https://app.test/verify-email", StringComparison.Ordinal));

    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync()
    {
        await using var db = TestDb.CreateContext();

        await db.Users
            .Where(u => u.Email == _email)
            .ExecuteDeleteAsync();
    }

    private sealed class CapturingEmailSender : IEmailSender
    {
        public List<(string To, string Subject, string Body)> Sent { get; } = new();

        public Task SendAsync(string to, string subject, string body, CancellationToken cancellationToken)
        {
            Sent.Add((to, subject, body));
            return Task.CompletedTask;
        }
    }

    private sealed class NullCurrentUser : ICurrentUser
    {
        public Guid? UserId => null;
    }
}