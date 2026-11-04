using Ferre.LinkManager.Application.Common.Abstractions;
using Ferre.LinkManager.Application.Common.Exceptions;

namespace Ferre.LinkManager.Application.Users.ConfirmEmail;

public sealed class ConfirmEmailHandler
{
    private const string InvalidTokenCode = "InvalidToken";

    private readonly IIdentityService _identityService;

    public ConfirmEmailHandler(IIdentityService identityService)
    {
        _identityService = identityService;
    }

    public async Task HandleAsync(ConfirmEmailCommand command, CancellationToken cancellationToken)
    {
        if (command.UserId == Guid.Empty || string.IsNullOrWhiteSpace(command.Token))
        {
            throw new ValidationException(new[] { InvalidTokenCode });
        }

        var result = await _identityService.ConfirmEmailAsync(command.UserId, command.Token, cancellationToken);

        if (!result.Succeeded)
        {
            throw new ValidationException(new[] { InvalidTokenCode });
        }
    }
}