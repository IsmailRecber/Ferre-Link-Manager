using Ferre.LinkManager.Application.Common.Exceptions;
using Ferre.LinkManager.Application.Tests.Fakes;
using Ferre.LinkManager.Application.Users.Register;
using Xunit;

namespace Ferre.LinkManager.Application.Tests.Users;

public class RegisterHandlerTests
{
    private readonly FakeIdentityService _identity = new();
    private readonly FakeEventPublisher _publisher = new();

    private RegisterHandler CreateHandler() => new(_identity, _publisher);

    [Fact]
    public async Task HandleAsync_WithValidData_PublishesUserRegisteredEvent()
    {
        var response = await CreateHandler().HandleAsync(
            new RegisterCommand("  user@example.com ", "Abc123"), CancellationToken.None);

        var published = Assert.IsType<UserRegistered>(Assert.Single(_publisher.Published));
        Assert.Equal(response.UserId, published.UserId);
        Assert.Equal("user@example.com", published.Email);
    }

    [Fact]
    public async Task HandleAsync_WhenIdentityFails_ThrowsValidationAndPublishesNothing()
    {
        _identity.RegisterSucceeds = false;

        var exception = await Assert.ThrowsAsync<ValidationException>(() =>
            CreateHandler().HandleAsync(new RegisterCommand("user@example.com", "Abc123"), CancellationToken.None));

        Assert.Contains("DuplicateEmail", exception.Errors);
        Assert.Empty(_publisher.Published);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task HandleAsync_WithEmptyEmail_ThrowsValidationWithoutCallingIdentity(string email)
    {
        var exception = await Assert.ThrowsAsync<ValidationException>(() =>
            CreateHandler().HandleAsync(new RegisterCommand(email, "Abc123"), CancellationToken.None));

        Assert.Contains("InvalidEmail", exception.Errors);
        Assert.Equal(0, _identity.RegisterCalls);
    }

    [Fact]
    public async Task HandleAsync_WithTooLongEmail_ThrowsValidation()
    {
        var email = new string('a', 250) + "@example.com";

        var exception = await Assert.ThrowsAsync<ValidationException>(() =>
            CreateHandler().HandleAsync(new RegisterCommand(email, "Abc123"), CancellationToken.None));

        Assert.Contains("InvalidEmail", exception.Errors);
    }

    [Fact]
    public async Task HandleAsync_WithEmptyPassword_ThrowsValidation()
    {
        var exception = await Assert.ThrowsAsync<ValidationException>(() =>
            CreateHandler().HandleAsync(new RegisterCommand("user@example.com", ""), CancellationToken.None));

        Assert.Contains("PasswordRequired", exception.Errors);
        Assert.Equal(0, _identity.RegisterCalls);
    }

    [Fact]
    public async Task HandleAsync_WithTooLongPassword_ThrowsValidation()
    {
        var exception = await Assert.ThrowsAsync<ValidationException>(() =>
            CreateHandler().HandleAsync(
                new RegisterCommand("user@example.com", new string('A', 129)), CancellationToken.None));

        Assert.Contains("PasswordTooLong", exception.Errors);
        Assert.Equal(0, _identity.RegisterCalls);
    }
}