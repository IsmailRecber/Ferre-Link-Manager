using Ferre.LinkManager.Application.Common.Abstractions;
using Ferre.LinkManager.Application.Users.Register;

namespace Ferre.LinkManager.Application.Users.SendVerificationEmail;

public sealed class SendVerificationEmailHandler : IEventHandler<UserRegistered>
{
    private readonly IIdentityService _identityService;
    private readonly IEmailVerificationLinkBuilder _linkBuilder;
    private readonly IEmailSender _emailSender;

    public SendVerificationEmailHandler(
        IIdentityService identityService,
        IEmailVerificationLinkBuilder linkBuilder,
        IEmailSender emailSender)
    {
        _identityService = identityService;
        _linkBuilder = linkBuilder;
        _emailSender = emailSender;
    }

    public Task HandleAsync(UserRegistered domainEvent, CancellationToken cancellationToken) =>
        SendAsync(domainEvent.UserId, domainEvent.Email, cancellationToken);

    public async Task SendAsync(Guid userId, string email, CancellationToken cancellationToken)
    {
        var token = await _identityService.GenerateEmailConfirmationTokenAsync(userId, cancellationToken);

        if (token is null)
        {
            return;
        }

        var link = _linkBuilder.Build(userId, token);

        await _emailSender.SendAsync(
            email,
            "Confirm your e-mail address",
            $"Welcome to Ferre Link Manager. Please confirm your e-mail address using this link: {link}",
            cancellationToken);
    }
}