using Ferre.LinkManager.Domain.Common;

namespace Ferre.LinkManager.Domain.Categories;

public sealed record CategoryCreated(Guid CategoryId, string Name) : IDomainEvent
{
    public DateTime OccurredOnUtc { get; } = DateTime.UtcNow;
}

public sealed record CategoryDeleted(Guid CategoryId, string Name) : IDomainEvent
{
    public DateTime OccurredOnUtc { get; } = DateTime.UtcNow;
}