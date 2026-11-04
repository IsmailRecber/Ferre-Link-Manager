using Ferre.LinkManager.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace Ferre.LinkManager.Infrastructure.Tests;

internal static class TestDb
{
    public static string GetConnectionString()
    {
        var configuration = new ConfigurationBuilder()
            .AddUserSecrets(typeof(TestDb).Assembly)
            .Build();

        return configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException(
                "Connection string 'DefaultConnection' was not found in the test project's user secrets.");
    }

    public static AppDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlServer(GetConnectionString())
            .Options;

        return new AppDbContext(options);
    }

    // Deletes ONLY the given records (and their audit logs). LinkRoleAccesses are removed by the database cascade.
    public static async Task CleanupAsync(IReadOnlyCollection<Guid> linkIds, IReadOnlyCollection<Guid> categoryIds)
    {
        await using var db = CreateContext();

        var entityIds = linkIds.Concat(categoryIds).ToList();

        await db.AuditLogs
            .Where(a => entityIds.Contains(a.EntityId))
            .ExecuteDeleteAsync();

        await db.Links.IgnoreQueryFilters()
            .Where(l => linkIds.Contains(l.Id))
            .ExecuteDeleteAsync();

        await db.Categories.IgnoreQueryFilters()
            .Where(c => categoryIds.Contains(c.Id))
            .ExecuteDeleteAsync();
    }
}