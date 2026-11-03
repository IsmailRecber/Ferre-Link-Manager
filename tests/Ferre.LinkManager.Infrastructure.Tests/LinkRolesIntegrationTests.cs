using Ferre.LinkManager.Domain.Categories;
using Ferre.LinkManager.Domain.Links;
using Ferre.LinkManager.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Ferre.LinkManager.Infrastructure.Tests;

public class LinkRolesIntegrationTests : IAsyncLifetime
{
    private readonly List<Guid> _linkIds = new();
    private readonly List<Guid> _categoryIds = new();

    [Fact]
    public async Task Update_KeepingOneRoleAndReplacingAnother_SavesWithoutIndexConflict()
    {
        var roleA = "TestRoleA-" + Guid.NewGuid();
        var roleB = "TestRoleB-" + Guid.NewGuid();
        var roleC = "TestRoleC-" + Guid.NewGuid();

        Guid linkId;
        Guid categoryId;

        await using (var db = TestDb.CreateContext())
        {
            var category = Category.Create("Test-" + Guid.NewGuid());
            _categoryIds.Add(category.Id);
            db.Categories.Add(category);

            var link = Link.Create("Roles", "https://roles.local", category.Id, new[] { roleA, roleB });
            _linkIds.Add(link.Id);
            db.Links.Add(link);

            await db.SaveChangesAsync();
            linkId = link.Id;
            categoryId = category.Id;
        }

        await using (var db = TestDb.CreateContext())
        {
            var repository = new LinkRepository(db);
            var loaded = await repository.GetByIdAsync(linkId, CancellationToken.None);
            Assert.NotNull(loaded);

            loaded.Update("Roles", "https://roles.local", categoryId, new[] { roleA, roleC });
            await db.SaveChangesAsync();
        }

        await using (var db = TestDb.CreateContext())
        {
            var roles = await db.Set<LinkRoleAccess>()
                .Where(a => a.LinkId == linkId)
                .Select(a => a.RoleName)
                .ToListAsync();

            Assert.Equal(
                new[] { roleA, roleC }.OrderBy(r => r, StringComparer.Ordinal),
                roles.OrderBy(r => r, StringComparer.Ordinal));
        }
    }

    public Task InitializeAsync() => Task.CompletedTask;

    public Task DisposeAsync() => TestDb.CleanupAsync(_linkIds, _categoryIds);
}