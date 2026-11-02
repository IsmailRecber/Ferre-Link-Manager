using Ferre.LinkManager.Application.Categories;
using Ferre.LinkManager.Application.Common.Abstractions;
using Ferre.LinkManager.Application.Links;
using Ferre.LinkManager.Domain.Categories;
using Ferre.LinkManager.Domain.Links;

namespace Ferre.LinkManager.Application.Tests.Fakes;

public sealed class FakeCategoryRepository : ICategoryRepository
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

public sealed class FakeLinkRepository : ILinkRepository
{
    public List<Link> Items { get; } = new();

    public Task AddAsync(Link link, CancellationToken cancellationToken)
    {
        Items.Add(link);
        return Task.CompletedTask;
    }

    public Task<Link?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        Task.FromResult(Items.FirstOrDefault(l => l.Id == id && !l.IsDeleted));

    public Task<bool> AnyByCategoryIdAsync(Guid categoryId, CancellationToken cancellationToken) =>
        Task.FromResult(Items.Any(l => l.CategoryId == categoryId && !l.IsDeleted));
}

public sealed class FakeUnitOfWork : IUnitOfWork
{
    public int SaveCount { get; private set; }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken)
    {
        SaveCount++;
        return Task.FromResult(1);
    }
}