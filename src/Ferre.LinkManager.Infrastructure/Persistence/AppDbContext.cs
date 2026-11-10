using Ferre.LinkManager.Application.Common.Abstractions;
using Ferre.LinkManager.Domain.Audit;
using Ferre.LinkManager.Domain.Categories;
using Ferre.LinkManager.Domain.Common;
using Ferre.LinkManager.Domain.Links;
using Ferre.LinkManager.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Ferre.LinkManager.Infrastructure.Persistence;

public class AppDbContext
    : IdentityDbContext<ApplicationUser, ApplicationRole, Guid>, IUnitOfWork, IReadDbContext
{
    private readonly IEventPublisher? _eventPublisher;

    public AppDbContext(DbContextOptions<AppDbContext> options, IEventPublisher? eventPublisher = null)
        : base(options)
    {
        _eventPublisher = eventPublisher;
    }

    public DbSet<Link> Links => Set<Link>();

    public DbSet<Category> Categories => Set<Category>();

    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    IQueryable<Link> IReadDbContext.Links => Links;

    IQueryable<Category> IReadDbContext.Categories => Categories;

    IQueryable<AuditLog> IReadDbContext.AuditLogs => AuditLogs;

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var aggregates = ChangeTracker.Entries<AggregateRoot>()
            .Select(entry => entry.Entity)
            .Where(aggregate => aggregate.DomainEvents.Count > 0)
            .ToList();

        var domainEvents = aggregates.SelectMany(aggregate => aggregate.DomainEvents).ToList();

        var result = await base.SaveChangesAsync(cancellationToken);

        foreach (var aggregate in aggregates)
        {
            aggregate.ClearDomainEvents();
        }

        if (_eventPublisher is not null)
        {
            foreach (var domainEvent in domainEvents)
            {
                await _eventPublisher.PublishAsync(domainEvent, cancellationToken);
            }
        }

        return result;
    }
}