using Ferre.LinkManager.Application.Common.Exceptions;
using Ferre.LinkManager.Application.Links.GetAllLinks;
using Ferre.LinkManager.Application.Links.GetLinkRedirect;
using Ferre.LinkManager.Domain.Categories;
using Ferre.LinkManager.Domain.Links;
using Xunit;

namespace Ferre.LinkManager.Infrastructure.Tests;

public class LinkQueriesIntegrationTests : IAsyncLifetime
{
    private readonly List<Guid> _linkIds = new();
    private readonly List<Guid> _categoryIds = new();
    private readonly string _role = "TestRole-" + Guid.NewGuid();

    private async Task<(Link Visible, Link Inactive)> SeedAsync()
    {
        await using var db = TestDb.CreateContext();

        var category = Category.Create("Test-" + Guid.NewGuid());
        _categoryIds.Add(category.Id);
        db.Categories.Add(category);

        var visible = Link.Create("Visible", "https://visible.example.com", category.Id, new[] { _role });
        var inactive = Link.Create("Inactive", "https://inactive.example.com", category.Id, new[] { _role });
        inactive.Deactivate();

        _linkIds.AddRange(new[] { visible.Id, inactive.Id });
        db.Links.AddRange(visible, inactive);
        await db.SaveChangesAsync();

        return (visible, inactive);
    }

    [Fact]
    public async Task Redirect_ForAuthorizedRole_ReturnsStoredUrl()
    {
        var (visible, _) = await SeedAsync();
        await using var db = TestDb.CreateContext();

        var url = await new GetLinkRedirectHandler(db).HandleAsync(
            new GetLinkRedirectQuery(visible.Id, new[] { _role }), CancellationToken.None);

        Assert.Equal("https://visible.example.com", url);
    }

    [Fact]
    public async Task Redirect_ForInactiveLink_ThrowsNotFound()
    {
        var (_, inactive) = await SeedAsync();
        await using var db = TestDb.CreateContext();

        await Assert.ThrowsAsync<NotFoundException>(() =>
            new GetLinkRedirectHandler(db).HandleAsync(
                new GetLinkRedirectQuery(inactive.Id, new[] { _role }), CancellationToken.None));
    }

    [Fact]
    public async Task Redirect_ForRoleWithoutAccess_ThrowsNotFound()
    {
        var (visible, _) = await SeedAsync();
        await using var db = TestDb.CreateContext();

        await Assert.ThrowsAsync<NotFoundException>(() =>
            new GetLinkRedirectHandler(db).HandleAsync(
                new GetLinkRedirectQuery(visible.Id, new[] { "Other-" + Guid.NewGuid() }), CancellationToken.None));
    }

    [Fact]
    public async Task AdminList_IncludesInactiveLinksWithUrlAndRoles()
    {
        var (visible, inactive) = await SeedAsync();
        await using var db = TestDb.CreateContext();

        var all = await new GetAllLinksHandler(db).HandleAsync(CancellationToken.None);

        var ours = all.Where(link => link.Id == visible.Id || link.Id == inactive.Id).ToList();
        Assert.Equal(2, ours.Count);

        var inactiveDto = Assert.Single(ours, link => link.Id == inactive.Id);
        Assert.False(inactiveDto.IsActive);
        Assert.Equal("https://inactive.example.com", inactiveDto.Url);
        Assert.Equal(_role, Assert.Single(inactiveDto.Roles));
    }

    public Task InitializeAsync() => Task.CompletedTask;

    public Task DisposeAsync() => TestDb.CleanupAsync(_linkIds, _categoryIds);
}