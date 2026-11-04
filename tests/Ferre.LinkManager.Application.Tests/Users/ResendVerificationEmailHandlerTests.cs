using Ferre.LinkManager.Application.Tests.Fakes;
using Ferre.LinkManager.Application.Users.ResendVerificationEmail;
using Ferre.LinkManager.Application.Users.SendVerificationEmail;
using Xunit;

namespace Ferre.LinkManager.Application.Tests.Users;

public class ResendVerificationEmailHandlerTests
{
    private readonly FakeIdentityService _identity = new();
    private readonly FakeEmailSender _emailSender = new();

    private ResendVerificationEmailHandler CreateHandler()
    {
        var sendHandler = new SendVerificationEmailHandler(
            _identity, new FakeEmailVerificationLinkBuilder(), _emailSender);

        return new ResendVerificationEmailHandler(_identity, sendHandler);
    }

    [Fact]
    public async Task HandleAsync_ForPendingUser_SendsEmail()
    {
        await CreateHandler().HandleAsync(new ResendVerificationEmailCommand(Guid.NewGuid()), CancellationToken.None);

        Assert.Single(_emailSender.Sent);
    }

    [Fact]
    public async Task HandleAsync_WhenUserIsNotPending_SendsNothingAndDoesNotThrow()
    {
        _identity.PendingEmail = null;

        await CreateHandler().HandleAsync(new ResendVerificationEmailCommand(Guid.NewGuid()), CancellationToken.None);

        Assert.Empty(_emailSender.Sent);
    }

    [Fact]
    public async Task HandleAsync_WithEmptyUserId_SendsNothing()
    {
        await CreateHandler().HandleAsync(new ResendVerificationEmailCommand(Guid.Empty), CancellationToken.None);

        Assert.Empty(_emailSender.Sent);
    }
}