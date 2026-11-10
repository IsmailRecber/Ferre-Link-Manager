using Ferre.LinkManager.Application.Common.Abstractions;
using Ferre.LinkManager.Domain.Audit;
using Ferre.LinkManager.Domain.Categories;
using Ferre.LinkManager.Domain.Links;

namespace Ferre.LinkManager.Application.Tests.Fakes;

/// <summary>An empty read model. Enough for tests that fail before any query is run.</summary>
public sealed class FakeReadDbContext : IReadDbContext
{
    public IQueryable<Link> Links => Enumerable.Empty<Link>().AsQueryable();

    public IQueryable<Category> Categories => Enumerable.Empty<Category>().AsQueryable();

    public IQueryable<AuditLog> AuditLogs => Enumerable.Empty<AuditLog>().AsQueryable();
}