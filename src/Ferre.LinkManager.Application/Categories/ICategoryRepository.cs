using Ferre.LinkManager.Domain.Categories;

namespace Ferre.LinkManager.Application.Categories;

public interface ICategoryRepository
{
    Task AddAsync(Category category, CancellationToken cancellationToken);

    Task<Category?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<bool> ExistsByNameAsync(CategoryName name, CancellationToken cancellationToken);
}