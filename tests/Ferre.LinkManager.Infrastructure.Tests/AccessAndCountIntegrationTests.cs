using Ferre.LinkManager.Application.Categories.GetCategories;
using Ferre.LinkManager.Application.Common.Exceptions;
using Ferre.LinkManager.Application.Links.GetLinkRedirect;
using Ferre.LinkManager.Application.Links.GetMyLinks;
using Ferre.LinkManager.Domain.Categories;
using Ferre.LinkManager.Domain.Links;
using Xunit;

namespace Ferre.LinkManager.Infrastructure.Tests;

public class AccessAndCountIntegrationTests : IAsyncLifetime
{
    private readonly List<Guid> _linkIds = new();
    private readonly List<Guid> _categoryIds = new();

    [Fact]
    public async Task AdminRole_SeesAndOpensEveryActiveLink_ButNotInactiveOnes()
    {
        var category = Category.Create("Test-" + Guid.NewGuid());
        _categoryIds.Add(category.Id);

        var userOnly = Link.Create("UserOnly", "https://user.example.com", category.Id, new[] { "User" });
        var inactive = Link.Create("Inactive", "https://inactive.example.com", category.Id, new[] { "User" });
        inactive.Deactivate();
        _linkIds.AddRange(new[] { userOnly.Id, inactive.Id });

        await using (var db = TestDb.CreateContext())
        {
            db.Categories.Add(category);
            db.Links.AddRange(userOnly, inactive);
            await db.SaveChangesAsync();
        }

        await using var readDb = TestDb.CreateContext();
        var admin = new[] { "Admin" };

        var mine = await new GetMyLinksHandler(readDb).HandleAsync(new GetMyLinksQuery(admin), CancellationToken.None);

        Assert.Contains(mine, link => link.Id == userOnly.Id);
        Assert.DoesNotContain(mine, link => link.Id == inactive.Id);

        var url = await new GetLinkRedirectHandler(readDb).HandleAsync(
            new GetLinkRedirectQuery(userOnly.Id, admin), CancellationToken.None);
        Assert.Equal("https://user.example.com", url);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            new GetLinkRedirectHandler(readDb).HandleAsync(
                new GetLinkRedirectQuery(inactive.Id, admin), CancellationToken.None));
    }

    [Fact]
    public async Task UserRole_DoesNotSeeLinksThatAreOnlyForAdmins()
    {
        var category = Category.Create("Test-" + Guid.NewGuid());
        _categoryIds.Add(category.Id);

        var adminOnly = Link.Create("AdminOnly", "https://admin.example.com", category.Id, new[] { "Admin" });
        _linkIds.Add(adminOnly.Id);

        await using (var db = TestDb.CreateContext())
        {
            db.Categories.Add(category);
            db.Links.Add(adminOnly);
            await db.SaveChangesAsync();
        }

        await using var readDb = TestDb.CreateContext();
        var user = new[] { "User" };

        var mine = await new GetMyLinksHandler(readDb).HandleAsync(new GetMyLinksQuery(user), CancellationToken.None);
        Assert.DoesNotContain(mine, link => link.Id == adminOnly.Id);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            new GetLinkRedirectHandler(readDb).HandleAsync(
                new GetLinkRedirectQuery(adminOnly.Id, user), CancellationToken.None));
    }

    [Fact]
    public async Task Categories_ReportTheirLinkCount_CountingInactiveButNotDeletedLinks()
    {
        var withLinks = Category.Create("Test-" + Guid.NewGuid());
        var empty = Category.Create("Test-" + Guid.NewGuid());
        _categoryIds.AddRange(new[] { withLinks.Id, empty.Id });

        var active = Link.Create("Active", "https://a.example.com", withLinks.Id, new[] { "User" });
        var inactive = Link.Create("Inactive", "https://b.example.com", withLinks.Id, new[] { "User" });
        inactive.Deactivate();
        var deleted = Link.Create("Deleted", "https://c.example.com", withLinks.Id, new[] { "User" });
        deleted.Delete();
        _linkIds.AddRange(new[] { active.Id, inactive.Id, deleted.Id });

        await using (var db = TestDb.CreateContext())
        {
            db.Categories.AddRange(withLinks, empty);
            db.Links.AddRange(active, inactive, deleted);
            await db.SaveChangesAsync();
        }

        await using var readDb = TestDb.CreateContext();
        var categories = await new GetCategoriesHandler(readDb).HandleAsync(CancellationToken.None);

        Assert.Equal(2, categories.Single(category => category.Id == withLinks.Id).LinkCount);
        Assert.Equal(0, categories.Single(category => category.Id == empty.Id).LinkCount);
    }

    public Task InitializeAsync() => Task.CompletedTask;

    public Task DisposeAsync() => TestDb.CleanupAsync(_linkIds, _categoryIds);
}