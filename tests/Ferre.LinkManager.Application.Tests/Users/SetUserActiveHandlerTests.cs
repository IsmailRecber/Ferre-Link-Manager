using Ferre.LinkManager.Application.Common.Abstractions;
using Ferre.LinkManager.Application.Common.Exceptions;
using Ferre.LinkManager.Application.Tests.Fakes;
using Ferre.LinkManager.Application.Users.SetUserActive;
using Xunit;

namespace Ferre.LinkManager.Application.Tests.Users;

public class SetUserActiveHandlerTests
{
    private readonly FakeIdentityService _identity = new();
    private readonly FakeEventPublisher _publisher = new();
    private readonly Guid _adminId = Guid.NewGuid();

    private SetUserActiveHandler CreateHandler() => new(_identity, new FakeCurrentUser(_adminId), _publisher);

    [Fact]
    public async Task Deactivate_AnotherUser_PublishesUserDeactivated()
    {
        var userId = Guid.NewGuid();

        await CreateHandler().HandleAsync(new SetUserActiveCommand(userId, false), CancellationToken.None);

        var published = Assert.IsType<UserDeactivated>(Assert.Single(_publisher.Published));
        Assert.Equal(userId, published.UserId);
    }

    [Fact]
    public async Task Activate_AnotherUser_PublishesUserActivated()
    {
        var userId = Guid.NewGuid();

        await CreateHandler().HandleAsync(new SetUserActiveCommand(userId, true), CancellationToken.None);

        var published = Assert.IsType<UserActivated>(Assert.Single(_publisher.Published));
        Assert.Equal(userId, published.UserId);
    }

    [Fact]
    public async Task Deactivate_Self_ThrowsConflictAndPublishesNothing()
    {
        await Assert.ThrowsAsync<ConflictException>(() =>
            CreateHandler().HandleAsync(new SetUserActiveCommand(_adminId, false), CancellationToken.None));

        Assert.Empty(_publisher.Published);
    }

    [Fact]
    public async Task WhenUserNotFound_ThrowsNotFound()
    {
        _identity.SetActiveResult = UserStateChange.NotFound;

        await Assert.ThrowsAsync<NotFoundException>(() =>
            CreateHandler().HandleAsync(new SetUserActiveCommand(Guid.NewGuid(), false), CancellationToken.None));

        Assert.Empty(_publisher.Published);
    }

    [Fact]
    public async Task WhenNothingChanged_PublishesNothing()
    {
        _identity.SetActiveResult = UserStateChange.Unchanged;

        await CreateHandler().HandleAsync(new SetUserActiveCommand(Guid.NewGuid(), false), CancellationToken.None);

        Assert.Empty(_publisher.Published);
    }
}