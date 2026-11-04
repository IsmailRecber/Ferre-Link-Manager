using Ferre.LinkManager.Application.Common.Exceptions;
using Ferre.LinkManager.Application.Tests.Fakes;
using Ferre.LinkManager.Application.Users.ConfirmEmail;
using Xunit;

namespace Ferre.LinkManager.Application.Tests.Users;

public class ConfirmEmailHandlerTests
{
    private readonly FakeIdentityService _identity = new();

    private ConfirmEmailHandler CreateHandler() => new(_identity);

    [Fact]
    public async Task HandleAsync_WithValidToken_Succeeds()
    {
        await CreateHandler().HandleAsync(
            new ConfirmEmailCommand(Guid.NewGuid(), "token-123"), CancellationToken.None);
    }

    [Fact]
    public async Task HandleAsync_WhenIdentityRejectsToken_ThrowsInvalidToken()
    {
        _identity.ConfirmSucceeds = false;

        var exception = await Assert.ThrowsAsync<ValidationException>(() =>
            CreateHandler().HandleAsync(new ConfirmEmailCommand(Guid.NewGuid(), "bad"), CancellationToken.None));

        Assert.Contains("InvalidToken", exception.Errors);
    }

    [Fact]
    public async Task HandleAsync_WithEmptyInput_ThrowsInvalidToken()
    {
        await Assert.ThrowsAsync<ValidationException>(() =>
            CreateHandler().HandleAsync(new ConfirmEmailCommand(Guid.Empty, ""), CancellationToken.None));
    }
}