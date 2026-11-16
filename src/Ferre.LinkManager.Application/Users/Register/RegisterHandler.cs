using Ferre.LinkManager.Application.Common.Abstractions;
using Ferre.LinkManager.Application.Common.Exceptions;

namespace Ferre.LinkManager.Application.Users.Register;

public sealed class RegisterHandler
{
    private const int MaxEmailLength = 256;
    private const int MaxPasswordLength = 128;

    private readonly IIdentityService _identityService;
    private readonly IEventPublisher _eventPublisher;

    public RegisterHandler(IIdentityService identityService, IEventPublisher eventPublisher)
    {
        _identityService = identityService;
        _eventPublisher = eventPublisher;
    }

    public async Task<RegisterResponse> HandleAsync(RegisterCommand command, CancellationToken cancellationToken)
    {
        var email = command.Email?.Trim();
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(email) || email.Length > MaxEmailLength)
        {
            errors.Add("InvalidEmail");
        }

        if (string.IsNullOrEmpty(command.Password))
        {
            errors.Add("PasswordRequired");
        }
        else if (command.Password.Length > MaxPasswordLength)
        {
            errors.Add("PasswordTooLong");
        }

        if (errors.Count > 0)
        {
            throw new ValidationException(errors);
        }

        var result = await _identityService.RegisterUserAsync(email!, command.Password, cancellationToken);

        if (!result.Succeeded || result.UserId is null)
        {
            throw new ValidationException(result.ErrorCodes);
        }


        await _eventPublisher.PublishAsync(new UserRegistered(result.UserId.Value, email!), cancellationToken);

        return new RegisterResponse(result.UserId.Value);
    }
}