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

    // Deleted categories and deleted links are hidden by the global query filter.
    // Inactive categories and inactive links are included (admin view).
    public async Task<IReadOnlyList<CategoryDto>> HandleAsync(CancellationToken cancellationToken)
    {
        var categories = await _db.Categories
            .OrderBy(category => category.Name)
            .Select(category => new { category.Id, Name = category.Name.Value, category.IsActive })
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        var counts = await _db.Links
            .GroupBy(link => link.CategoryId)
            .Select(group => new { CategoryId = group.Key, Count = group.Count() })
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        var countByCategory = counts.ToDictionary(item => item.CategoryId, item => item.Count);

        return categories
            .Select(category => new CategoryDto(
                category.Id,
                category.Name,
                category.IsActive,
                countByCategory.GetValueOrDefault(category.Id)))
            .ToList();
    }
}