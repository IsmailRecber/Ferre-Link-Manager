using Ferre.LinkManager.Application.Common.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace Ferre.LinkManager.Application.Links.GetAllLinks;

public sealed class GetAllLinksHandler
{
    private readonly IReadDbContext _db;

    public GetAllLinksHandler(IReadDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<AdminLinkDto>> HandleAsync(CancellationToken cancellationToken)
    {
        return await (
                from link in _db.Links
                join category in _db.Categories on link.CategoryId equals category.Id
                orderby category.Name, link.Title
                select new AdminLinkDto(
                    link.Id,
                    link.Title,
                    link.Url.Value,
                    link.CategoryId,
                    category.Name.Value,
                    link.IsActive,
                    link.RoleAccesses.Select(access => access.RoleName).ToList()))
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }
}