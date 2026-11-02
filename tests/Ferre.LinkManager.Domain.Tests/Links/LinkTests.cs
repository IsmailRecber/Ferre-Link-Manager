using Ferre.LinkManager.Domain.Common;
using Ferre.LinkManager.Domain.Links;
using Xunit;

namespace Ferre.LinkManager.Domain.Tests.Links;

public class LinkTests
{
    private static readonly Guid CategoryId = Guid.NewGuid();

    private static Link CreateValidLink() =>
        Link.Create("Payroll", "https://payroll.local", CategoryId, new[] { "Admin", "User" });

    [Fact]
    public void Create_WithValidData_StartsActiveAndRaisesCreatedEvent()
    {
        var link = CreateValidLink();

        Assert.Equal("Payroll", link.Title);
        Assert.Equal("https://payroll.local", link.Url.Value);
        Assert.Equal(CategoryId, link.CategoryId);
        Assert.True(link.IsActive);
        Assert.False(link.IsDeleted);
        Assert.Equal(2, link.RoleAccesses.Count);

        var domainEvent = Assert.IsType<LinkCreated>(Assert.Single(link.DomainEvents));
        Assert.Equal(link.Id, domainEvent.LinkId);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithEmptyTitle_Throws(string? title)
    {
        Assert.Throws<DomainException>(() =>
            Link.Create(title, "https://example.com", CategoryId, new[] { "User" }));
    }

    [Fact]
    public void Create_WithTooLongTitle_Throws()
    {
        var title = new string('a', Link.MaxTitleLength + 1);

        Assert.Throws<DomainException>(() =>
            Link.Create(title, "https://example.com", CategoryId, new[] { "User" }));
    }

    [Fact]
    public void Create_WithInvalidUrl_Throws()
    {
        Assert.Throws<DomainException>(() =>
            Link.Create("Test", "file:///C:/x", CategoryId, new[] { "User" }));
    }

    [Fact]
    public void Create_WithEmptyCategoryId_Throws()
    {
        Assert.Throws<DomainException>(() =>
            Link.Create("Test", "https://example.com", Guid.Empty, new[] { "User" }));
    }

    [Fact]
    public void Create_WithoutRoles_Throws()
    {
        Assert.Throws<DomainException>(() =>
            Link.Create("Test", "https://example.com", CategoryId, Array.Empty<string>()));
        Assert.Throws<DomainException>(() =>
            Link.Create("Test", "https://example.com", CategoryId, null));
        Assert.Throws<DomainException>(() =>
            Link.Create("Test", "https://example.com", CategoryId, new string?[] { " ", null }));
    }

    [Fact]
    public void Create_WithDuplicateRolesIgnoringCase_KeepsOne()
    {
        var link = Link.Create("Test", "https://example.com", CategoryId, new[] { "User", "user", " USER " });

        Assert.Single(link.RoleAccesses);
    }

    [Fact]
    public void Create_WithTooLongRoleName_Throws()
    {
        var role = new string('r', LinkRoleAccess.MaxRoleNameLength + 1);

        Assert.Throws<DomainException>(() =>
            Link.Create("Test", "https://example.com", CategoryId, new[] { role }));
    }

    [Fact]
    public void Update_WithValidData_ChangesValuesAndRaisesUpdatedEvent()
    {
        var link = CreateValidLink();
        var newCategoryId = Guid.NewGuid();

        link.Update("HR Portal", "https://hr.local", newCategoryId, new[] { "Admin" });

        Assert.Equal("HR Portal", link.Title);
        Assert.Equal("https://hr.local", link.Url.Value);
        Assert.Equal(newCategoryId, link.CategoryId);
        Assert.Single(link.RoleAccesses);
        Assert.Contains(link.DomainEvents, e => e is LinkUpdated);
    }

    [Fact]
    public void Update_WithInvalidUrl_DoesNotChangeAnything()
    {
        var link = CreateValidLink();

        Assert.Throws<DomainException>(() =>
            link.Update("New title", "javascript:alert(1)", CategoryId, new[] { "Admin" }));

        Assert.Equal("Payroll", link.Title);
        Assert.Equal("https://payroll.local", link.Url.Value);
        Assert.Equal(2, link.RoleAccesses.Count);
    }

    [Fact]
    public void Update_WithoutRoles_DoesNotChangeAnything()
    {
        var link = CreateValidLink();

        Assert.Throws<DomainException>(() =>
            link.Update("New title", "https://new.local", CategoryId, Array.Empty<string>()));

        Assert.Equal("Payroll", link.Title);
        Assert.Equal(2, link.RoleAccesses.Count);
    }

    [Fact]
    public void Deactivate_ThenActivate_TogglesIsActive()
    {
        var link = CreateValidLink();

        link.Deactivate();
        Assert.False(link.IsActive);

        link.Activate();
        Assert.True(link.IsActive);
    }

    [Fact]
    public void Delete_MarksAsDeletedAndRaisesDeletedEventWithSnapshot()
    {
        var link = CreateValidLink();

        link.Delete();

        Assert.True(link.IsDeleted);
        var deleted = Assert.IsType<LinkDeleted>(link.DomainEvents.Last());
        Assert.Equal("Payroll", deleted.Title);
        Assert.Equal("https://payroll.local", deleted.Url);
    }

    [Fact]
    public void Delete_WhenAlreadyDeleted_Throws()
    {
        var link = CreateValidLink();
        link.Delete();

        Assert.Throws<DomainException>(() => link.Delete());
    }

    [Fact]
    public void Update_WhenDeleted_Throws()
    {
        var link = CreateValidLink();
        link.Delete();

        Assert.Throws<DomainException>(() =>
            link.Update("X", "https://x.local", CategoryId, new[] { "User" }));
    }

    [Theory]
    [InlineData("user", true)]
    [InlineData("ADMIN", true)]
    [InlineData("Guest", false)]
    public void IsAccessibleTo_ChecksRolesCaseInsensitively(string role, bool expected)
    {
        var link = CreateValidLink();

        Assert.Equal(expected, link.IsAccessibleTo(new[] { role }));
    }

    [Fact]
    public void IsAccessibleTo_WithNoRoles_ReturnsFalse()
    {
        var link = CreateValidLink();

        Assert.False(link.IsAccessibleTo(Array.Empty<string>()));
    }
}