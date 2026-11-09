using Ferre.LinkManager.Application.Common.Exceptions;
using Ferre.LinkManager.Application.Links.SetLinkActive;
using Ferre.LinkManager.Application.Tests.Fakes;
using Ferre.LinkManager.Domain.Links;
using Xunit;

namespace Ferre.LinkManager.Application.Tests.Links;

public class SetLinkActiveHandlerTests
{
    private readonly FakeLinkRepository _links = new();
    private readonly FakeUnitOfWork _unitOfWork = new();

    private SetLinkActiveHandler CreateHandler() => new(_links, _unitOfWork);

    private Link AddLink()
    {
        var link = Link.Create("Payroll", "https://payroll.local", Guid.NewGuid(), new[] { "User" });
        _links.Items.Add(link);
        return link;
    }

    [Fact]
    public async Task Deactivate_ActiveLink_DeactivatesAndSaves()
    {
        var link = AddLink();

        await CreateHandler().HandleAsync(new SetLinkActiveCommand(link.Id, false), CancellationToken.None);

        Assert.False(link.IsActive);
        Assert.Equal(1, _unitOfWork.SaveCount);
    }

    [Fact]
    public async Task Activate_InactiveLink_ActivatesAndSaves()
    {
        var link = AddLink();
        link.Deactivate();

        await CreateHandler().HandleAsync(new SetLinkActiveCommand(link.Id, true), CancellationToken.None);

        Assert.True(link.IsActive);
        Assert.Equal(1, _unitOfWork.SaveCount);
    }

    [Fact]
    public async Task WhenAlreadyInRequestedState_DoesNotSave()
    {
        var link = AddLink();

        await CreateHandler().HandleAsync(new SetLinkActiveCommand(link.Id, true), CancellationToken.None);

        Assert.True(link.IsActive);
        Assert.Equal(0, _unitOfWork.SaveCount);
    }

    [Fact]
    public async Task WhenLinkNotFound_ThrowsNotFound()
    {
        await Assert.ThrowsAsync<NotFoundException>(() =>
            CreateHandler().HandleAsync(new SetLinkActiveCommand(Guid.NewGuid(), false), CancellationToken.None));

        Assert.Equal(0, _unitOfWork.SaveCount);
    }
}