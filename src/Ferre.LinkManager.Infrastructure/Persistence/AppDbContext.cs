using Ferre.LinkManager.Application.Common.Abstractions;
using Ferre.LinkManager.Domain.Categories;
using Ferre.LinkManager.Domain.Links;
using Ferre.LinkManager.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Ferre.LinkManager.Infrastructure.Persistence;

public class AppDbContext
    : IdentityDbContext<ApplicationUser, ApplicationRole, Guid>, IUnitOfWork, IReadDbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<Link> Links => Set<Link>();

    public DbSet<Category> Categories => Set<Category>();

    IQueryable<Link> IReadDbContext.Links => Links;

    IQueryable<Category> IReadDbContext.Categories => Categories;

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}