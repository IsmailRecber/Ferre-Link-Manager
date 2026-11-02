using Ferre.LinkManager.Application.Common.Exceptions;
using Ferre.LinkManager.Application.Links.DeleteLink;
using Ferre.LinkManager.Application.Tests.Fakes;
using Ferre.LinkManager.Domain.Links;
using Xunit;

namespace Ferre.LinkManager.Application.Tests.Links;

public class DeleteLinkHandlerTests
{
    private readonly FakeLinkRepository _links = new();
    private readonly FakeUnitOfWork _unitOfWork = new();

    private DeleteLinkHandler CreateHandler() => new(_links, _unitOfWork);

    [Fact]
    public async Task HandleAsync_WithExistingLink_DeletesAndSaves()
    {
        var link = Link.Create("Payroll", "https://payroll.local", Guid.NewGuid(), new[] { "User" });
        _links.Items.Add(link);

        await CreateHandler().HandleAsync(new DeleteLinkCommand(link.Id), CancellationToken.None);

        Assert.True(link.IsDeleted);
        Assert.Contains(link.DomainEvents, e => e is LinkDeleted);
        Assert.Equal(1, _unitOfWork.SaveCount);
    }

    [Fact]
    public async Task HandleAsync_WhenLinkNotFound_ThrowsNotFound()
    {
        await Assert.ThrowsAsync<NotFoundException>(() =>
            CreateHandler().HandleAsync(new DeleteLinkCommand(Guid.NewGuid()), CancellationToken.None));

        Assert.Equal(0, _unitOfWork.SaveCount);
    }
}