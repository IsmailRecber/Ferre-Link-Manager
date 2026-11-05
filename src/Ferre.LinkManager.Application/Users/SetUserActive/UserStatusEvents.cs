using Ferre.LinkManager.Domain.Common;

namespace Ferre.LinkManager.Application.Users.SetUserActive;

public sealed record UserActivated(Guid UserId) : IDomainEvent
{
    public DateTime OccurredOnUtc { get; } = DateTime.UtcNow;
}

public sealed record UserDeactivated(Guid UserId) : IDomainEvent
{
    public DateTime OccurredOnUtc { get; } = DateTime.UtcNow;
}