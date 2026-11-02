using Ferre.LinkManager.Application.Categories;
using Ferre.LinkManager.Application.Common.Abstractions;
using Ferre.LinkManager.Application.Common.Exceptions;
using Ferre.LinkManager.Application.Links;
using Ferre.LinkManager.Application.Links.CreateLink;
using Ferre.LinkManager.Domain.Categories;
using Ferre.LinkManager.Domain.Common;
using Ferre.LinkManager.Domain.Links;
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