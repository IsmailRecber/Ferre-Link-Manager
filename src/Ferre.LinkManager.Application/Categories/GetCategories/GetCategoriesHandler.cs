using Ferre.LinkManager.Application.Common.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace Ferre.LinkManager.Application.Categories.GetCategories;

public sealed class GetCategoriesHandler
{
    private readonly IReadDbContext _db;

    public GetCategoriesHandler(IReadDbContext db)
    {
        _db = db;
    }

    // Deleted categories are hidden by the global query filter. Inactive ones are included (admin view).
    public async Task<IReadOnlyList<CategoryDto>> HandleAsync(CancellationToken cancellationToken)
    {
        return await _db.Categories
            .OrderBy(category => category.Name)
            .Select(category => new CategoryDto(category.Id, category.Name.Value, category.IsActive))
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }
}