using Ferre.LinkManager.Application.Links.GetMyLinks;
using Ferre.LinkManager.Domain.Categories;
using Ferre.LinkManager.Domain.Links;
using Xunit;

namespace Ferre.LinkManager.Infrastructure.Tests;

public class GetMyLinksIntegrationTests : IAsyncLifetime
{
    private readonly List<Guid> _linkIds = new();
    private readonly List<Guid> _categoryIds = new();
    private readonly string _role = "TestRole-" + Guid.NewGuid();

    [Fact]
    public async Task HandleAsync_ReturnsOnlyLinksVisibleToTheRole()
    {
        await using var db = TestDb.CreateContext();

        var activeCategory = Category.Create("Test-" + Guid.NewGuid());
        var inactiveCategory = Category.Create("Test-" + Guid.NewGuid());
        inactiveCategory.Deactivate();
        _categoryIds.AddRange(new[] { activeCategory.Id, inactiveCategory.Id });
        db.Categories.AddRange(activeCategory, inactiveCategory);

        var visible = Link.Create("Visible", "https://visible.local", activeCategory.Id, new[] { _role });

        var inactiveLink = Link.Create("Inactive", "https://inactive.local", activeCategory.Id, new[] { _role });
        inactiveLink.Deactivate();

        var otherRole = Link.Create("OtherRole", "https://other.local", activeCategory.Id,
            new[] { "Other-" + Guid.NewGuid() });

        var inInactiveCategory = Link.Create("InInactiveCategory", "https://x.local", inactiveCategory.Id,
            new[] { _role });

        var deletedLink = Link.Create("Deleted", "https://deleted.local", activeCategory.Id, new[] { _role });
        deletedLink.Delete();

        var links = new[] { visible, inactiveLink, otherRole, inInactiveCategory, deletedLink };
        _linkIds.AddRange(links.Select(l => l.Id));
        db.Links.AddRange(links);
        await db.SaveChangesAsync();

        await using var readDb = TestDb.CreateContext();
        var handler = new GetMyLinksHandler(readDb);

        var result = await handler.HandleAsync(new GetMyLinksQuery(new[] { _role }), CancellationToken.None);

        var item = Assert.Single(result);
        Assert.Equal(visible.Id, item.Id);
        Assert.Equal("Visible", item.Title);
        Assert.Equal(activeCategory.Name.Value, item.CategoryName);
    }

    public Task InitializeAsync() => Task.CompletedTask;

    public Task DisposeAsync() => TestDb.CleanupAsync(_linkIds, _categoryIds);
}