using Ferre.LinkManager.Application.Common.Abstractions;
using Ferre.LinkManager.Application.Common.Authorization;
using Microsoft.EntityFrameworkCore;

namespace Ferre.LinkManager.Application.Links.GetMyLinks;

public sealed class GetMyLinksHandler
{
    private readonly IReadDbContext _db;

    public GetMyLinksHandler(IReadDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<LinkDto>> HandleAsync(
        GetMyLinksQuery query,
        CancellationToken cancellationToken)
    {
        var roles = query.Roles.ToArray();

        if (roles.Length == 0)
        {
            return Array.Empty<LinkDto>();
        }

        // Admins see every active link. Other roles see only the links that were given to them.
        var seesEverything = roles.Contains(Roles.Admin, StringComparer.OrdinalIgnoreCase);

        return await (
                from link in _db.Links
                join category in _db.Categories on link.CategoryId equals category.Id
                where link.IsActive && !link.IsDeleted
                      && category.IsActive && !category.IsDeleted
                      && (seesEverything || link.RoleAccesses.Any(access => roles.Contains(access.RoleName)))
                orderby category.Name, link.Title
                select new LinkDto(link.Id, link.Title, link.CategoryId, category.Name.Value))
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }
}