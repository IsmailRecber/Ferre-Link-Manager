using Ferre.LinkManager.Application.Common.Abstractions;
using Ferre.LinkManager.Application.Common.Exceptions;
using Ferre.LinkManager.Application.Users.SendVerificationEmail;

namespace Ferre.LinkManager.Application.Users.ResendVerificationByEmail;

public sealed class ResendVerificationByEmailHandler
{
    private const int MaxEmailLength = 256;

    private readonly IIdentityService _identityService;
    private readonly SendVerificationEmailHandler _sendHandler;

    public ResendVerificationByEmailHandler(
        IIdentityService identityService,
        SendVerificationEmailHandler sendHandler)
    {
        _identityService = identityService;
        _sendHandler = sendHandler;
    }

    // Only a malformed request is reported. Whether an account exists is never revealed:
    // an unknown, confirmed or inactive account completes silently, exactly like a pending one.
    public async Task HandleAsync(ResendVerificationByEmailCommand command, CancellationToken cancellationToken)
    {
        var email = command.Email?.Trim();

        if (string.IsNullOrWhiteSpace(email) || email.Length > MaxEmailLength)
        {
            throw new ValidationException(new[] { "InvalidEmail" });
        }

        var pending = await _identityService.FindPendingConfirmationUserAsync(email, cancellationToken);

        if (pending is null)
        {
            return;
        }

        await _sendHandler.SendAsync(pending.UserId, pending.Email, cancellationToken);
    }
}