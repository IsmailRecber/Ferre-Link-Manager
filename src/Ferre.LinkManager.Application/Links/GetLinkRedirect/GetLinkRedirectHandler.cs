using Ferre.LinkManager.Application.Common.Abstractions;
using Ferre.LinkManager.Application.Common.Authorization;
using Ferre.LinkManager.Application.Common.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace Ferre.LinkManager.Application.Links.GetLinkRedirect;

public sealed class GetLinkRedirectHandler
{
    private readonly IReadDbContext _db;

    public GetLinkRedirectHandler(IReadDbContext db)
    {
        _db = db;
    }

    /// <summary>
    /// Returns the stored destination of a link the caller is allowed to open.
    /// Every failure is reported as "not found" so the existence of a link is never revealed.
    /// </summary>
    public async Task<string> HandleAsync(GetLinkRedirectQuery query, CancellationToken cancellationToken)
    {
        var roles = query.Roles.ToArray();

        if (query.Id == Guid.Empty || roles.Length == 0)
        {
            throw new NotFoundException("Link was not found.");
        }

        // Admins may open every active link. Other roles only the links that were given to them.
        var seesEverything = roles.Contains(Roles.Admin, StringComparer.OrdinalIgnoreCase);

        var url = await (
                from link in _db.Links
                join category in _db.Categories on link.CategoryId equals category.Id
                where link.Id == query.Id
                      && link.IsActive && !link.IsDeleted
                      && category.IsActive && !category.IsDeleted
                      && (seesEverything || link.RoleAccesses.Any(access => roles.Contains(access.RoleName)))
                select link.Url)
            .AsNoTracking()
            .FirstOrDefaultAsync(cancellationToken);

        if (url is null)
        {
            throw new NotFoundException("Link was not found.");
        }

        // Defense in depth: the domain already validates the URL, but never redirect to anything but HTTP(S).
        if (!Uri.TryCreate(url.Value, UriKind.Absolute, out var uri)
            || (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
        {
            throw new NotFoundException("Link was not found.");
        }

        return url.Value;
    }
}