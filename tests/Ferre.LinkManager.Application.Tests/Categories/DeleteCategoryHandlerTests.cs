using Ferre.LinkManager.Application.Categories;
using Ferre.LinkManager.Application.Categories.DeleteCategory;
using Ferre.LinkManager.Application.Common.Abstractions;
using Ferre.LinkManager.Application.Common.Exceptions;
using Ferre.LinkManager.Application.Links;
using Ferre.LinkManager.Domain.Categories;
using Ferre.LinkManager.Domain.Links;
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

    private sealed class FakeCategoryRepository : ICategoryRepository
    {
        public List<Category> Items { get; } = new();

        public Task AddAsync(Category category, CancellationToken cancellationToken)
        {
            Items.Add(category);
            return Task.CompletedTask;
        }

        public Task<Category?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
            Task.FromResult(Items.FirstOrDefault(c => c.Id == id && !c.IsDeleted));

        public Task<bool> ExistsByNameAsync(CategoryName name, CancellationToken cancellationToken) =>
            Task.FromResult(Items.Any(c => c.Name == name));
    }

    private sealed class FakeLinkRepository : ILinkRepository
    {
        public List<Link> Items { get; } = new();

        public Task AddAsync(Link link, CancellationToken cancellationToken)
        {
            Items.Add(link);
            return Task.CompletedTask;
        }

        public Task<bool> AnyByCategoryIdAsync(Guid categoryId, CancellationToken cancellationToken) =>
            Task.FromResult(Items.Any(l => l.CategoryId == categoryId && !l.IsDeleted));
    }

    private sealed class FakeUnitOfWork : IUnitOfWork
    {
        public int SaveCount { get; private set; }

        public Task<int> SaveChangesAsync(CancellationToken cancellationToken)
        {
            SaveCount++;
            return Task.FromResult(1);
        }
    }
}