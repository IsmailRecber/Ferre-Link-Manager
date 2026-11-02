using Ferre.LinkManager.Application.Categories;
using Ferre.LinkManager.Application.Categories.CreateCategory;
using Ferre.LinkManager.Application.Common.Abstractions;
using Ferre.LinkManager.Application.Common.Exceptions;
using Ferre.LinkManager.Domain.Categories;
using Ferre.LinkManager.Domain.Common;
using Xunit;

namespace Ferre.LinkManager.Application.Tests.Categories;

public class CreateCategoryHandlerTests
{
    private readonly FakeCategoryRepository _repository = new();
    private readonly FakeUnitOfWork _unitOfWork = new();

    private CreateCategoryHandler CreateHandler() => new(_repository, _unitOfWork);

    [Fact]
    public async Task HandleAsync_WithValidName_AddsCategoryAndSaves()
    {
        var response = await CreateHandler().HandleAsync(
            new CreateCategoryCommand("Finance"), CancellationToken.None);

        Assert.Equal("Finance", response.Name);
        Assert.NotEqual(Guid.Empty, response.Id);
        Assert.Single(_repository.Categories);
        Assert.Equal(1, _unitOfWork.SaveCount);
    }

    [Fact]
    public async Task HandleAsync_WithDuplicateName_ThrowsConflictAndDoesNotSave()
    {
        await CreateHandler().HandleAsync(new CreateCategoryCommand("Finance"), CancellationToken.None);

        await Assert.ThrowsAsync<ConflictException>(() =>
            CreateHandler().HandleAsync(new CreateCategoryCommand("Finance"), CancellationToken.None));

        Assert.Single(_repository.Categories);
        Assert.Equal(1, _unitOfWork.SaveCount);
    }

    [Fact]
    public async Task HandleAsync_WithInvalidName_ThrowsDomainExceptionAndDoesNotSave()
    {
        await Assert.ThrowsAsync<DomainException>(() =>
            CreateHandler().HandleAsync(new CreateCategoryCommand("  "), CancellationToken.None));

        Assert.Empty(_repository.Categories);
        Assert.Equal(0, _unitOfWork.SaveCount);
    }

    private sealed class FakeCategoryRepository : ICategoryRepository
    {
        public List<Category> Categories { get; } = new();

        public Task AddAsync(Category category, CancellationToken cancellationToken)
        {
            Categories.Add(category);
            return Task.CompletedTask;
        }

        public Task<Category?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
            Task.FromResult(Categories.FirstOrDefault(c => c.Id == id));

        public Task<bool> ExistsByNameAsync(CategoryName name, CancellationToken cancellationToken) =>
            Task.FromResult(Categories.Any(c => c.Name == name));
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