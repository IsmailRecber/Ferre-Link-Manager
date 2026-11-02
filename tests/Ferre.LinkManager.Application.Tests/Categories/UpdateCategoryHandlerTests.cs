using Ferre.LinkManager.Application.Categories.UpdateCategory;
using Ferre.LinkManager.Application.Common.Exceptions;
using Ferre.LinkManager.Application.Tests.Fakes;
using Ferre.LinkManager.Domain.Categories;
using Ferre.LinkManager.Domain.Common;
using Xunit;

namespace Ferre.LinkManager.Application.Tests.Categories;

public class UpdateCategoryHandlerTests
{
    private readonly FakeCategoryRepository _categories = new();
    private readonly FakeUnitOfWork _unitOfWork = new();

    private UpdateCategoryHandler CreateHandler() => new(_categories, _unitOfWork);

    private Category AddCategory(string name)
    {
        var category = Category.Create(name);
        _categories.Items.Add(category);
        return category;
    }

    [Fact]
    public async Task HandleAsync_WithNewName_RenamesAndSaves()
    {
        var category = AddCategory("Finance");

        await CreateHandler().HandleAsync(
            new UpdateCategoryCommand(category.Id, "Accounting", true), CancellationToken.None);

        Assert.Equal("Accounting", category.Name.Value);
        Assert.Equal(1, _unitOfWork.SaveCount);
    }

    [Fact]
    public async Task HandleAsync_WithSameName_DoesNotThrowConflict()
    {
        var category = AddCategory("Finance");

        await CreateHandler().HandleAsync(
            new UpdateCategoryCommand(category.Id, "Finance", true), CancellationToken.None);

        Assert.Equal("Finance", category.Name.Value);
        Assert.Equal(1, _unitOfWork.SaveCount);
    }

    [Fact]
    public async Task HandleAsync_WithIsActiveFalse_DeactivatesCategory()
    {
        var category = AddCategory("Finance");

        await CreateHandler().HandleAsync(
            new UpdateCategoryCommand(category.Id, "Finance", false), CancellationToken.None);

        Assert.False(category.IsActive);
    }

    [Fact]
    public async Task HandleAsync_WithIsActiveTrue_ReactivatesCategory()
    {
        var category = AddCategory("Finance");
        category.Deactivate();

        await CreateHandler().HandleAsync(
            new UpdateCategoryCommand(category.Id, "Finance", true), CancellationToken.None);

        Assert.True(category.IsActive);
    }

    [Fact]
    public async Task HandleAsync_WhenCategoryNotFound_ThrowsNotFound()
    {
        await Assert.ThrowsAsync<NotFoundException>(() =>
            CreateHandler().HandleAsync(
                new UpdateCategoryCommand(Guid.NewGuid(), "Finance", true), CancellationToken.None));

        Assert.Equal(0, _unitOfWork.SaveCount);
    }

    [Fact]
    public async Task HandleAsync_WithNameUsedByAnotherCategory_ThrowsConflict()
    {
        var finance = AddCategory("Finance");
        AddCategory("HR");

        await Assert.ThrowsAsync<ConflictException>(() =>
            CreateHandler().HandleAsync(
                new UpdateCategoryCommand(finance.Id, "HR", true), CancellationToken.None));

        Assert.Equal("Finance", finance.Name.Value);
        Assert.Equal(0, _unitOfWork.SaveCount);
    }

    [Fact]
    public async Task HandleAsync_WithInvalidName_ThrowsDomainException()
    {
        var category = AddCategory("Finance");

        await Assert.ThrowsAsync<DomainException>(() =>
            CreateHandler().HandleAsync(
                new UpdateCategoryCommand(category.Id, "  ", true), CancellationToken.None));

        Assert.Equal("Finance", category.Name.Value);
        Assert.Equal(0, _unitOfWork.SaveCount);
    }
}