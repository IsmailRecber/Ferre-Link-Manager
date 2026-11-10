using Ferre.LinkManager.Domain.Audit;
using Ferre.LinkManager.Domain.Categories;
using Ferre.LinkManager.Domain.Links;

namespace Ferre.LinkManager.Application.Common.Abstractions;

/// <summary>
/// Read-only access to the data for queries. Implemented by Infrastructure.
/// </summary>
public interface IReadDbContext
{
    IQueryable<Link> Links { get; }

    IQueryable<Category> Categories { get; }

    IQueryable<AuditLog> AuditLogs { get; }
}