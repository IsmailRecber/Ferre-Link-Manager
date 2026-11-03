using Ferre.LinkManager.Application.Links;
using Ferre.LinkManager.Domain.Links;
using Microsoft.EntityFrameworkCore;

namespace Ferre.LinkManager.Infrastructure.Persistence;

public sealed class LinkRepository : ILinkRepository
{
    private readonly AppDbContext _db;

    public LinkRepository(AppDbContext db)
    {
        _db = db;
    }

    public async Task AddAsync(Link link, CancellationToken cancellationToken)
    {
        await _db.Links.AddAsync(link, cancellationToken);
    }

    public Task<Link?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return _db.Links
            .Include(l => l.RoleAccesses)
            .FirstOrDefaultAsync(l => l.Id == id, cancellationToken);
    }

    public Task<bool> AnyByCategoryIdAsync(Guid categoryId, CancellationToken cancellationToken)
    {
        return _db.Links.AnyAsync(l => l.CategoryId == categoryId, cancellationToken);
    }
}