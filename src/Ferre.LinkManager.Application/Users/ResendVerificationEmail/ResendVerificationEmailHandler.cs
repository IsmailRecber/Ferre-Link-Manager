using Ferre.LinkManager.Application.Common.Abstractions;
using Ferre.LinkManager.Application.Users.SendVerificationEmail;

namespace Ferre.LinkManager.Application.Users.ResendVerificationEmail;

public sealed class ResendVerificationEmailHandler
{
    private readonly IIdentityService _identityService;
    private readonly SendVerificationEmailHandler _sendHandler;

    public ResendVerificationEmailHandler(
        IIdentityService identityService,
        SendVerificationEmailHandler sendHandler)
    {
        _identityService = identityService;
        _sendHandler = sendHandler;
    }

    public async Task HandleAsync(ResendVerificationEmailCommand command, CancellationToken cancellationToken)
    {
        if (command.UserId == Guid.Empty)
        {
            return;
        }

        var email = await _identityService.GetPendingConfirmationEmailAsync(command.UserId, cancellationToken);

        if (email is null)
        {
            return;
        }

        await _sendHandler.SendAsync(command.UserId, email, cancellationToken);
    }
}