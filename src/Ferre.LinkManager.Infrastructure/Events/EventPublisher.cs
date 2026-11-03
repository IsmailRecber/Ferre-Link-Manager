using Ferre.LinkManager.Application.Common.Abstractions;
using Ferre.LinkManager.Domain.Common;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Ferre.LinkManager.Infrastructure.Events;

public sealed class EventPublisher : IEventPublisher
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<EventPublisher> _logger;

    public EventPublisher(IServiceProvider serviceProvider, ILogger<EventPublisher> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public async Task PublishAsync(IDomainEvent domainEvent, CancellationToken cancellationToken)
    {
        var eventType = domainEvent.GetType();
        var handlerType = typeof(IEventHandler<>).MakeGenericType(eventType);
        var handlers = _serviceProvider.GetServices(handlerType);

        foreach (var handler in handlers)
        {
            try
            {
                var method = handlerType.GetMethod(nameof(IEventHandler<IDomainEvent>.HandleAsync))!;
                await (Task)method.Invoke(handler, new object[] { domainEvent, cancellationToken })!;
            }
            catch (Exception ex)
            {
                // The data is already saved. A failing handler must not turn a successful operation into an error.
                _logger.LogError(ex,
                    "Event handler {Handler} failed for event {EventType}.",
                    handler!.GetType().Name,
                    eventType.Name);
            }
        }
    }
}