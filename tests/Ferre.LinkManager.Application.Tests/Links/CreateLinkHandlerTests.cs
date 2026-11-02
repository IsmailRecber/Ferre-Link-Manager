using Ferre.LinkManager.Application.Categories;
using Ferre.LinkManager.Application.Common.Abstractions;
using Ferre.LinkManager.Application.Common.Exceptions;
using Ferre.LinkManager.Application.Links;
using Ferre.LinkManager.Application.Links.CreateLink;
using Ferre.LinkManager.Domain.Categories;
using Ferre.LinkManager.Domain.Common;
using Ferre.LinkManager.Domain.Links;
using Ferre.LinkManager.Application.Tests.Fakes;
using Xunit;

namespace Ferre.LinkManager.Application.Tests.Links;

public class CreateLinkHandlerTests
{
    private readonly FakeCategoryRepository _categories = new();
    private readonly FakeLinkRepository _links = new();
    private readonly FakeUnitOfWork _unitOfWork = new();

    private CreateLinkHandler CreateHandler() => new(_links, _categories, _unitOfWork);

    private static CreateLinkCommand ValidCommand(Guid categoryId) =>
        new("Payroll", "https://payroll.local", categoryId, new[] { "Admin", "User" });

    [Fact]
    public async Task HandleAsync_WithValidData_AddsLinkAndSaves()
    {
        var category = Category.Create("Finance");
        _categories.Items.Add(category);

        var response = await CreateHandler().HandleAsync(ValidCommand(category.Id), CancellationToken.None);

        Assert.Equal("Payroll", response.Title);
        Assert.Equal(category.Id, response.CategoryId);
        Assert.Single(_links.Items);
        Assert.Equal(1, _unitOfWork.SaveCount);
    }

    [Fact]
    public async Task HandleAsync_WhenCategoryNotFound_ThrowsNotFound()
    {
        await Assert.ThrowsAsync<NotFoundException>(() =>
            CreateHandler().HandleAsync(ValidCommand(Guid.NewGuid()), CancellationToken.None));

        Assert.Empty(_links.Items);
        Assert.Equal(0, _unitOfWork.SaveCount);
    }

    [Fact]
    public async Task HandleAsync_WhenCategoryInactive_ThrowsConflict()
    {
        var category = Category.Create("Finance");
        category.Deactivate();
        _categories.Items.Add(category);

        await Assert.ThrowsAsync<ConflictException>(() =>
            CreateHandler().HandleAsync(ValidCommand(category.Id), CancellationToken.None));

        Assert.Empty(_links.Items);
        Assert.Equal(0, _unitOfWork.SaveCount);
    }

    [Fact]
    public async Task HandleAsync_WithInvalidUrl_ThrowsDomainExceptionAndDoesNotSave()
    {
        var category = Category.Create("Finance");
        _categories.Items.Add(category);
        var command = new CreateLinkCommand("Test", "file:///C:/x", category.Id, new[] { "User" });

        await Assert.ThrowsAsync<DomainException>(() =>
            CreateHandler().HandleAsync(command, CancellationToken.None));

        Assert.Empty(_links.Items);
        Assert.Equal(0, _unitOfWork.SaveCount);
    }

    [Fact]
    public async Task HandleAsync_WithoutRoles_ThrowsDomainExceptionAndDoesNotSave()
    {
        var category = Category.Create("Finance");
        _categories.Items.Add(category);
        var command = new CreateLinkCommand("Test", "https://example.com", category.Id, Array.Empty<string>());

        await Assert.ThrowsAsync<DomainException>(() =>
            CreateHandler().HandleAsync(command, CancellationToken.None));

        Assert.Equal(0, _unitOfWork.SaveCount);
    }

   
}