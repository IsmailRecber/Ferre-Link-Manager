using Ferre.LinkManager.Application.Categories;
using Ferre.LinkManager.Domain.Categories;
using Microsoft.EntityFrameworkCore;

namespace Ferre.LinkManager.Infrastructure.Persistence;

public sealed class CategoryRepository : ICategoryRepository
{
    private readonly AppDbContext _db;

    public CategoryRepository(AppDbContext db)
    {
        _db = db;
    }

    public async Task AddAsync(Category category, CancellationToken cancellationToken)
    {
        await _db.Categories.AddAsync(category, cancellationToken);
    }

    public Task<Category?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return _db.Categories.FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
    }

    public Task<bool> ExistsByNameAsync(CategoryName name, CancellationToken cancellationToken)
    {
        return _db.Categories.AnyAsync(c => c.Name == name, cancellationToken);
    }
}