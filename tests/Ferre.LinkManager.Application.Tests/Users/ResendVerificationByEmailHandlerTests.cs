using Ferre.LinkManager.Application.Common.Exceptions;
using Ferre.LinkManager.Application.Tests.Fakes;
using Ferre.LinkManager.Application.Users.ResendVerificationByEmail;
using Ferre.LinkManager.Application.Users.SendVerificationEmail;
using Xunit;

namespace Ferre.LinkManager.Application.Tests.Users;

public class ResendVerificationByEmailHandlerTests
{
    private readonly FakeIdentityService _identity = new();
    private readonly FakeEmailSender _emailSender = new();

    private ResendVerificationByEmailHandler CreateHandler()
    {
        var sendHandler = new SendVerificationEmailHandler(
            _identity, new FakeEmailVerificationLinkBuilder(), _emailSender);

        return new ResendVerificationByEmailHandler(_identity, sendHandler);
    }

    [Fact]
    public async Task HandleAsync_ForPendingUser_SendsEmailToTheStoredAddress()
    {
        await CreateHandler().HandleAsync(
            new ResendVerificationByEmailCommand("  USER@example.com "), CancellationToken.None);

        var sent = Assert.Single(_emailSender.Sent);
        Assert.Equal("user@example.com", sent.To);
    }

    [Fact]
    public async Task HandleAsync_WhenNoPendingUser_SendsNothingAndDoesNotThrow()
    {
        _identity.PendingUser = null;

        await CreateHandler().HandleAsync(
            new ResendVerificationByEmailCommand("nobody@example.com"), CancellationToken.None);

        Assert.Empty(_emailSender.Sent);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task HandleAsync_WithBlankEmail_ThrowsValidation(string email)
    {
        var exception = await Assert.ThrowsAsync<ValidationException>(() =>
            CreateHandler().HandleAsync(new ResendVerificationByEmailCommand(email), CancellationToken.None));

        Assert.Contains("InvalidEmail", exception.Errors);
        Assert.Empty(_emailSender.Sent);
    }

    [Fact]
    public async Task HandleAsync_WithTooLongEmail_ThrowsValidation()
    {
        var email = new string('a', 250) + "@example.com";

        await Assert.ThrowsAsync<ValidationException>(() =>
            CreateHandler().HandleAsync(new ResendVerificationByEmailCommand(email), CancellationToken.None));
    }
}