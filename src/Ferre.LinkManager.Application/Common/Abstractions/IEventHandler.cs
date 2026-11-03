using Ferre.LinkManager.Domain.Common;

namespace Ferre.LinkManager.Application.Common.Abstractions;

public interface IEventHandler<in TEvent> where TEvent : IDomainEvent
{
    Task HandleAsync(TEvent domainEvent, CancellationToken cancellationToken);
}