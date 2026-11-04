using Ferre.LinkManager.Domain.Common;

namespace Ferre.LinkManager.Application.Users.Register;

public sealed record UserRegistered(Guid UserId, string Email) : IDomainEvent
{
    public DateTime OccurredOnUtc { get; } = DateTime.UtcNow;
}