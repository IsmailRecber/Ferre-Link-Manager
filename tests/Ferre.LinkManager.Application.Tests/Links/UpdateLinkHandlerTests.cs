using Ferre.LinkManager.Application.Common.Exceptions;
using Ferre.LinkManager.Application.Links.UpdateLink;
using Ferre.LinkManager.Application.Tests.Fakes;
using Ferre.LinkManager.Domain.Categories;
using Ferre.LinkManager.Domain.Common;
using Ferre.LinkManager.Domain.Links;
using Xunit;

namespace Ferre.LinkManager.Application.Tests.Links;

public class UpdateLinkHandlerTests
{
    private readonly FakeCategoryRepository _categories = new();
    private readonly FakeLinkRepository _links = new();
    private readonly FakeUnitOfWork _unitOfWork = new();

    private UpdateLinkHandler CreateHandler() => new(_links, _categories, _unitOfWork);

    private (Category Category, Link Link) AddCategoryWithLink()
    {
        var category = Category.Create("Finance");
        _categories.Items.Add(category);
        var link = Link.Create("Payroll", "https://payroll.local", category.Id, new[] { "User" });
        _links.Items.Add(link);
        return (category, link);
    }

    [Fact]
    public async Task HandleAsync_WithValidData_UpdatesLinkAndSaves()
    {
        var (category, link) = AddCategoryWithLink();
        var command = new UpdateLinkCommand(link.Id, "HR Portal", "https://hr.local", category.Id, new[] { "Admin" });

        await CreateHandler().HandleAsync(command, CancellationToken.None);

        Assert.Equal("HR Portal", link.Title);
        Assert.Equal("https://hr.local", link.Url.Value);
        Assert.Equal(1, _unitOfWork.SaveCount);
    }

    [Fact]
    public async Task HandleAsync_WhenLinkNotFound_ThrowsNotFound()
    {
        var category = Category.Create("Finance");
        _categories.Items.Add(category);
        var command = new UpdateLinkCommand(Guid.NewGuid(), "X", "https://x.local", category.Id, new[] { "User" });

        await Assert.ThrowsAsync<NotFoundException>(() =>
            CreateHandler().HandleAsync(command, CancellationToken.None));

        Assert.Equal(0, _unitOfWork.SaveCount);
    }

    [Fact]
    public async Task HandleAsync_WhenCategoryNotFound_ThrowsNotFound()
    {
        var (_, link) = AddCategoryWithLink();
        var command = new UpdateLinkCommand(link.Id, "X", "https://x.local", Guid.NewGuid(), new[] { "User" });

        await Assert.ThrowsAsync<NotFoundException>(() =>
            CreateHandler().HandleAsync(command, CancellationToken.None));

        Assert.Equal(0, _unitOfWork.SaveCount);
    }

    [Fact]
    public async Task HandleAsync_MovingToInactiveCategory_ThrowsConflict()
    {
        var (_, link) = AddCategoryWithLink();
        var inactive = Category.Create("Archive");
        inactive.Deactivate();
        _categories.Items.Add(inactive);
        var command = new UpdateLinkCommand(link.Id, "Payroll", "https://payroll.local", inactive.Id, new[] { "User" });

        await Assert.ThrowsAsync<ConflictException>(() =>
            CreateHandler().HandleAsync(command, CancellationToken.None));

        Assert.Equal(0, _unitOfWork.SaveCount);
    }

    [Fact]
    public async Task HandleAsync_WithInvalidUrl_ThrowsDomainExceptionAndKeepsOldValues()
    {
        var (category, link) = AddCategoryWithLink();
        var command = new UpdateLinkCommand(link.Id, "New", "javascript:alert(1)", category.Id, new[] { "User" });

        await Assert.ThrowsAsync<DomainException>(() =>
            CreateHandler().HandleAsync(command, CancellationToken.None));

        Assert.Equal("Payroll", link.Title);
        Assert.Equal(0, _unitOfWork.SaveCount);
    }
}