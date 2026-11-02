using Ferre.LinkManager.Application.Categories;
using Ferre.LinkManager.Application.Categories.DeleteCategory;
using Ferre.LinkManager.Application.Common.Abstractions;
using Ferre.LinkManager.Application.Common.Exceptions;
using Ferre.LinkManager.Application.Links;
using Ferre.LinkManager.Domain.Categories;
using Ferre.LinkManager.Domain.Links;
using Ferre.LinkManager.Application.Tests.Fakes;
using Xunit;

namespace Ferre.LinkManager.Application.Tests.Categories;

public class DeleteCategoryHandlerTests
{
    private readonly FakeCategoryRepository _categories = new();
    private readonly FakeLinkRepository _links = new();
    private readonly FakeUnitOfWork _unitOfWork = new();

    private DeleteCategoryHandler CreateHandler() => new(_categories, _links, _unitOfWork);

    [Fact]
    public async Task HandleAsync_WithoutLinks_DeletesCategoryAndSaves()
    {
        var category = Category.Create("Finance");
        _categories.Items.Add(category);

        await CreateHandler().HandleAsync(new DeleteCategoryCommand(category.Id), CancellationToken.None);

        Assert.True(category.IsDeleted);
        Assert.Equal(1, _unitOfWork.SaveCount);
    }

    [Fact]
    public async Task HandleAsync_WhenCategoryNotFound_ThrowsNotFound()
    {
        await Assert.ThrowsAsync<NotFoundException>(() =>
            CreateHandler().HandleAsync(new DeleteCategoryCommand(Guid.NewGuid()), CancellationToken.None));

        Assert.Equal(0, _unitOfWork.SaveCount);
    }

    [Fact]
    public async Task HandleAsync_WhenCategoryHasLinks_ThrowsConflictAndDoesNotDelete()
    {
        var category = Category.Create("Finance");
        _categories.Items.Add(category);
        _links.Items.Add(Link.Create("Payroll", "https://payroll.local", category.Id, new[] { "User" }));

        await Assert.ThrowsAsync<ConflictException>(() =>
            CreateHandler().HandleAsync(new DeleteCategoryCommand(category.Id), CancellationToken.None));

        Assert.False(category.IsDeleted);
        Assert.Equal(0, _unitOfWork.SaveCount);
    }

    [Fact]
    public async Task HandleAsync_WhenCategoryHasOnlyInactiveLinks_StillThrowsConflict()
    {
        var category = Category.Create("Finance");
        _categories.Items.Add(category);
        var link = Link.Create("Payroll", "https://payroll.local", category.Id, new[] { "User" });
        link.Deactivate();
        _links.Items.Add(link);

        await Assert.ThrowsAsync<ConflictException>(() =>
            CreateHandler().HandleAsync(new DeleteCategoryCommand(category.Id), CancellationToken.None));

        Assert.False(category.IsDeleted);
    }

}