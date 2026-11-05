using System.Net;
using System.Text.Json;
using Ferre.LinkManager.Application.Common.Authorization;
using Ferre.LinkManager.Infrastructure.Identity;
using Ferre.LinkManager.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Ferre.LinkManager.Api.IntegrationTests;

public class ApiSecurityTests : IClassFixture<TestApiFactory>, IAsyncLifetime
{
    private const string Password = "Abcdef1";

    private readonly TestApiFactory _factory;
    private readonly string _adminEmail = $"test-admin-{Guid.NewGuid()}@example.test";
    private readonly string _userEmail = $"test-user-{Guid.NewGuid()}@example.test";
    private readonly string _unconfirmedEmail = $"test-unconfirmed-{Guid.NewGuid()}@example.test";

    private readonly List<Guid> _userIds = new();
    private readonly List<Guid> _categoryIds = new();
    private readonly List<Guid> _linkIds = new();

    public ApiSecurityTests(TestApiFactory factory)
    {
        _factory = factory;
    }

    // ---------- Authentication ----------

    [Theory]
    [InlineData("/api/auth/me")]
    [InlineData("/api/links/my")]
    [InlineData("/api/links")]
    [InlineData("/api/categories")]
    [InlineData("/api/users")]
    public async Task Anonymous_IsRejected_WithUnauthorized(string url)
    {
        var client = _factory.CreateApiClient();

        var response = await client.GetAsync(url);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Login_WithWrongPassword_ReturnsUnauthorized()
    {
        var client = _factory.CreateApiClient();

        var response = await client.SendAsync(
            HttpMethod.Post, "/api/auth/login", new { email = _userEmail, password = "Wrong123" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Login_WithUnconfirmedEmail_ReturnsForbidden()
    {
        var client = _factory.CreateApiClient();

        var response = await client.SendAsync(
            HttpMethod.Post, "/api/auth/login", new { email = _unconfirmedEmail, password = Password });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Logout_EndsTheSession()
    {
        var client = await LoginAsync(_userEmail);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/api/auth/me")).StatusCode);

        var logout = await client.SendAsync(HttpMethod.Post, "/api/auth/logout", new { });
        Assert.Equal(HttpStatusCode.NoContent, logout.StatusCode);

        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/auth/me")).StatusCode);
    }

    // ---------- Authorization ----------

    [Theory]
    [InlineData("/api/links")]
    [InlineData("/api/categories")]
    [InlineData("/api/users")]
    public async Task User_IsForbidden_OnAdminReadEndpoints(string url)
    {
        var user = await LoginAsync(_userEmail);

        var response = await user.GetAsync(url);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task User_IsForbidden_ToCreateCategory()
    {
        var user = await LoginAsync(_userEmail);

        var response = await user.SendAsync(
            HttpMethod.Post, "/api/categories", new { name = "Test-" + Guid.NewGuid() });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    // ---------- Antiforgery ----------

    [Fact]
    public async Task Admin_WithoutAntiforgeryToken_IsRejected()
    {
        var admin = await LoginAsync(_adminEmail);

        var response = await admin.SendAsync(
            HttpMethod.Post, "/api/categories", new { name = "Test-" + Guid.NewGuid() }, withAntiforgery: false);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // ---------- Admin management rules ----------

    [Fact]
    public async Task Admin_CreateCategory_DuplicateNameConflicts()
    {
        var admin = await LoginAsync(_adminEmail);
        var name = "Test-" + Guid.NewGuid();

        var first = await admin.SendAsync(HttpMethod.Post, "/api/categories", new { name });
        Assert.Equal(HttpStatusCode.OK, first.StatusCode);
        _categoryIds.Add(await ReadIdAsync(first));

        var second = await admin.SendAsync(HttpMethod.Post, "/api/categories", new { name });
        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
    }

    [Fact]
    public async Task Admin_CreateLink_RejectsUnsafeUrlAndUnknownRole()
    {
        var admin = await LoginAsync(_adminEmail);
        var categoryId = await CreateCategoryAsync(admin);

        var fileUrl = await admin.SendAsync(HttpMethod.Post, "/api/links",
            new { title = "Bad", url = "file:///C:/x", categoryId, roles = new[] { "User" } });
        Assert.Equal(HttpStatusCode.BadRequest, fileUrl.StatusCode);

        var unknownRole = await admin.SendAsync(HttpMethod.Post, "/api/links",
            new { title = "Bad", url = "https://example.com", categoryId, roles = new[] { "Foo" } });
        Assert.Equal(HttpStatusCode.BadRequest, unknownRole.StatusCode);

        var noRoles = await admin.SendAsync(HttpMethod.Post, "/api/links",
            new { title = "Bad", url = "https://example.com", categoryId, roles = Array.Empty<string>() });
        Assert.Equal(HttpStatusCode.BadRequest, noRoles.StatusCode);
    }

    // ---------- Safe redirect and visibility ----------

    [Fact]
    public async Task Go_RedirectsAuthorizedUser_AndHidesLinkFromOthers()
    {
        var admin = await LoginAsync(_adminEmail);
        var categoryId = await CreateCategoryAsync(admin);
        var linkId = await CreateLinkAsync(admin, categoryId, "https://example.com", "User");

        var user = await LoginAsync(_userEmail);

        // The user's role is allowed: redirected to the URL stored in the database.
        var allowed = await user.GetAsync($"/api/links/{linkId}/go");
        Assert.Equal(HttpStatusCode.Redirect, allowed.StatusCode);
        Assert.Equal("https://example.com", allowed.Headers.Location!.OriginalString);

        // The admin only has the Admin role, which this link does not allow: it looks like it does not exist.
        var denied = await admin.GetAsync($"/api/links/{linkId}/go");
        Assert.Equal(HttpStatusCode.NotFound, denied.StatusCode);

        // Not signed in.
        var anonymous = await _factory.CreateApiClient().GetAsync($"/api/links/{linkId}/go");
        Assert.Equal(HttpStatusCode.Unauthorized, anonymous.StatusCode);

        // Unknown id.
        var unknown = await user.GetAsync($"/api/links/{Guid.NewGuid()}/go");
        Assert.Equal(HttpStatusCode.NotFound, unknown.StatusCode);
    }

    [Fact]
    public async Task MyLinks_ShowsTheLinkButNeverItsUrl()
    {
        var admin = await LoginAsync(_adminEmail);
        var categoryId = await CreateCategoryAsync(admin);
        var linkId = await CreateLinkAsync(admin, categoryId, "https://example.com", "User");

        var user = await LoginAsync(_userEmail);
        var response = await user.GetAsync("/api/links/my");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var item = json.RootElement.EnumerateArray()
            .Single(element => element.GetProperty("id").GetGuid() == linkId);

        Assert.Equal("Example", item.GetProperty("title").GetString());
        Assert.False(item.TryGetProperty("url", out _));
    }

    // ---------- Helpers ----------

    private async Task<ApiClient> LoginAsync(string email)
    {
        var client = _factory.CreateApiClient();

        var response = await client.SendAsync(
            HttpMethod.Post, "/api/auth/login", new { email, password = Password });

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        return client;
    }

    private async Task<Guid> CreateCategoryAsync(ApiClient admin)
    {
        var response = await admin.SendAsync(
            HttpMethod.Post, "/api/categories", new { name = "Test-" + Guid.NewGuid() });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var id = await ReadIdAsync(response);
        _categoryIds.Add(id);

        return id;
    }

    private async Task<Guid> CreateLinkAsync(ApiClient admin, Guid categoryId, string url, string role)
    {
        var response = await admin.SendAsync(HttpMethod.Post, "/api/links",
            new { title = "Example", url, categoryId, roles = new[] { role } });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var id = await ReadIdAsync(response);
        _linkIds.Add(id);

        return id;
    }

    private static async Task<Guid> ReadIdAsync(HttpResponseMessage response)
    {
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());

        return json.RootElement.GetProperty("id").GetGuid();
    }

    private async Task CreateUserAsync(string email, string role, bool emailConfirmed)
    {
        using var scope = _factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = email,
            Email = email,
            EmailConfirmed = emailConfirmed,
            IsActive = true
        };

        var created = await userManager.CreateAsync(user, Password);
        Assert.True(created.Succeeded, string.Join(", ", created.Errors.Select(e => e.Code)));
        _userIds.Add(user.Id);

        var inRole = await userManager.AddToRoleAsync(user, role);
        Assert.True(inRole.Succeeded);
    }

    public async Task InitializeAsync()
    {
        await CreateUserAsync(_adminEmail, Roles.Admin, emailConfirmed: true);
        await CreateUserAsync(_userEmail, Roles.User, emailConfirmed: true);
        await CreateUserAsync(_unconfirmedEmail, Roles.User, emailConfirmed: false);
    }

    // Deletes only what this test created: audit logs, links, categories, then the test users.
    public async Task DisposeAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var userIds = _userIds.ToArray();
        var linkIds = _linkIds.ToArray();
        var categoryIds = _categoryIds.ToArray();
        var entityIds = userIds.Concat(linkIds).Concat(categoryIds).ToArray();

        await db.AuditLogs
            .Where(a => entityIds.Contains(a.EntityId) || (a.UserId.HasValue && userIds.Contains(a.UserId.Value)))
            .ExecuteDeleteAsync();

        await db.Links.IgnoreQueryFilters().Where(l => linkIds.Contains(l.Id)).ExecuteDeleteAsync();
        await db.Categories.IgnoreQueryFilters().Where(c => categoryIds.Contains(c.Id)).ExecuteDeleteAsync();
        await db.Users.Where(u => userIds.Contains(u.Id)).ExecuteDeleteAsync();
    }
}