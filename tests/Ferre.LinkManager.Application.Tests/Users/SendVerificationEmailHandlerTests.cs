using Ferre.LinkManager.Application.Tests.Fakes;
using Ferre.LinkManager.Application.Users.Register;
using Ferre.LinkManager.Application.Users.SendVerificationEmail;
using Xunit;

namespace Ferre.LinkManager.Application.Tests.Users;

public class SendVerificationEmailHandlerTests
{
    private readonly FakeIdentityService _identity = new();
    private readonly FakeEmailSender _emailSender = new();

    private SendVerificationEmailHandler CreateHandler() =>
        new(_identity, new FakeEmailVerificationLinkBuilder(), _emailSender);

    [Fact]
    public async Task HandleAsync_SendsEmailWithVerificationLinkToTheUser()
    {
        var userId = Guid.NewGuid();

        await CreateHandler().HandleAsync(new UserRegistered(userId, "user@example.com"), CancellationToken.None);

        var sent = Assert.Single(_emailSender.Sent);
        Assert.Equal("user@example.com", sent.To);
        Assert.Contains("token-123", sent.Body);
        Assert.Contains(userId.ToString(), sent.Body);
    }

    [Fact]
    public async Task HandleAsync_WhenNoTokenCanBeGenerated_SendsNothing()
    {
        _identity.Token = null;

        await CreateHandler().HandleAsync(
            new UserRegistered(Guid.NewGuid(), "user@example.com"), CancellationToken.None);

        Assert.Empty(_emailSender.Sent);
    }
}