using Ferre.LinkManager.Domain.Common;

namespace Ferre.LinkManager.Application.Common.Abstractions;

public interface IEventPublisher
{
    Task PublishAsync(IDomainEvent domainEvent, CancellationToken cancellationToken);
}