using Ferre.LinkManager.Application.Common.Abstractions;
using Ferre.LinkManager.Application.Common.Exceptions;
using Ferre.LinkManager.Domain.Common;

namespace Ferre.LinkManager.Application.Users.SetUserActive;

public sealed class SetUserActiveHandler
{
    private readonly IIdentityService _identityService;
    private readonly ICurrentUser _currentUser;
    private readonly IEventPublisher _eventPublisher;

    public SetUserActiveHandler(
        IIdentityService identityService,
        ICurrentUser currentUser,
        IEventPublisher eventPublisher)
    {
        _identityService = identityService;
        _currentUser = currentUser;
        _eventPublisher = eventPublisher;
    }

    public async Task HandleAsync(SetUserActiveCommand command, CancellationToken cancellationToken)
    {
        // An admin must not lock themselves out.
        if (!command.IsActive && _currentUser.UserId == command.UserId)
        {
            throw new ConflictException("You cannot deactivate your own account.");
        }

        var change = await _identityService.SetUserActiveAsync(command.UserId, command.IsActive, cancellationToken);

        if (change == UserStateChange.NotFound)
        {
            throw new NotFoundException($"User '{command.UserId}' was not found.");
        }

        // Nothing changed, so there is nothing to audit.
        if (change == UserStateChange.Unchanged)
        {
            return;
        }

        IDomainEvent domainEvent = command.IsActive
            ? new UserActivated(command.UserId)
            : new UserDeactivated(command.UserId);

        await _eventPublisher.PublishAsync(domainEvent, cancellationToken);
    }
}