using Ferre.LinkManager.Domain.Common;

namespace Ferre.LinkManager.Domain.Links;

public sealed record LinkCreated(Guid LinkId, string Title, string Url) : IDomainEvent
{
    public DateTime OccurredOnUtc { get; } = DateTime.UtcNow;
}

public sealed record LinkUpdated(Guid LinkId, string Title, string Url) : IDomainEvent
{
    public DateTime OccurredOnUtc { get; } = DateTime.UtcNow;
}

public sealed record LinkDeleted(Guid LinkId, string Title, string Url) : IDomainEvent
{
    public DateTime OccurredOnUtc { get; } = DateTime.UtcNow;
}