using Ferre.LinkManager.Application.Common.Abstractions;
using Ferre.LinkManager.Domain.Common;

namespace Ferre.LinkManager.Application.Tests.Fakes;

public sealed class FakeIdentityService : IIdentityService
{
    public bool RegisterSucceeds { get; set; } = true;
    public string? Token { get; set; } = "token-123";
    public bool ConfirmSucceeds { get; set; } = true;
    public string? PendingEmail { get; set; } = "user@example.com";
    public int RegisterCalls { get; private set; }

    public Task<RegisterUserResult> RegisterUserAsync(string email, string password, CancellationToken cancellationToken)
    {
        RegisterCalls++;

        var result = RegisterSucceeds
            ? new RegisterUserResult(true, Guid.NewGuid(), Array.Empty<string>())
            : new RegisterUserResult(false, null, new[] { "DuplicateEmail" });

        return Task.FromResult(result);
    }

    public Task<string?> GenerateEmailConfirmationTokenAsync(Guid userId, CancellationToken cancellationToken) =>
        Task.FromResult(Token);

    public Task<ConfirmEmailResult> ConfirmEmailAsync(Guid userId, string token, CancellationToken cancellationToken) =>
        Task.FromResult(new ConfirmEmailResult(ConfirmSucceeds));

    public Task<string?> GetPendingConfirmationEmailAsync(Guid userId, CancellationToken cancellationToken) =>
        Task.FromResult(PendingEmail);
}

public sealed class FakeEventPublisher : IEventPublisher
{
    public List<IDomainEvent> Published { get; } = new();

    public Task PublishAsync(IDomainEvent domainEvent, CancellationToken cancellationToken)
    {
        Published.Add(domainEvent);
        return Task.CompletedTask;
    }
}

public sealed class FakeEmailSender : IEmailSender
{
    public List<(string To, string Subject, string Body)> Sent { get; } = new();

    public Task SendAsync(string to, string subject, string body, CancellationToken cancellationToken)
    {
        Sent.Add((to, subject, body));
        return Task.CompletedTask;
    }
}

public sealed class FakeEmailVerificationLinkBuilder : IEmailVerificationLinkBuilder
{
    public string Build(Guid userId, string token) => $"https://app.test/verify?userId={userId}&token={token}";
}