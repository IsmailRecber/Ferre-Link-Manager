using Ferre.LinkManager.Application.Common.Abstractions;
using Ferre.LinkManager.Domain.Common;

namespace Ferre.LinkManager.Application.Tests.Fakes;

public sealed class FakeIdentityService : IIdentityService
{
    public bool RegisterSucceeds { get; set; } = true;
    public string? Token { get; set; } = "token-123";
    public bool ConfirmSucceeds { get; set; } = true;
    public string? PendingEmail { get; set; } = "user@example.com";

    public PendingConfirmationUser? PendingUser { get; set; } = new(Guid.NewGuid(), "user@example.com");
    public UserStateChange SetActiveResult { get; set; } = UserStateChange.Changed;
    public List<UserSummary> Users { get; } = new();
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
    public Task<PendingConfirmationUser?> FindPendingConfirmationUserAsync(
    string email, CancellationToken cancellationToken) =>
    Task.FromResult(PendingUser);

    public Task<IReadOnlyDictionary<Guid, string>> GetEmailsAsync(
    IReadOnlyCollection<Guid> userIds, CancellationToken cancellationToken) =>
    Task.FromResult<IReadOnlyDictionary<Guid, string>>(new Dictionary<Guid, string>());

    public Task<IReadOnlyList<UserSummary>> GetUsersAsync(CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<UserSummary>>(Users);

    public Task<UserStateChange> SetUserActiveAsync(Guid userId, bool isActive, CancellationToken cancellationToken) =>
        Task.FromResult(SetActiveResult);
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

public sealed class FakeCurrentUser : ICurrentUser
{
    public FakeCurrentUser(Guid? userId) => UserId = userId;

    public Guid? UserId { get; }
}