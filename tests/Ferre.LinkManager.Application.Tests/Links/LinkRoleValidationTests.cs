using Ferre.LinkManager.Application.Common.Exceptions;
using Ferre.LinkManager.Application.Links.CreateLink;
using Ferre.LinkManager.Application.Links.UpdateLink;
using Ferre.LinkManager.Application.Tests.Fakes;
using Ferre.LinkManager.Domain.Categories;
using Ferre.LinkManager.Domain.Links;
using Xunit;

namespace Ferre.LinkManager.Application.Tests.Links;

public class LinkRoleValidationTests
{
    private readonly FakeCategoryRepository _categories = new();
    private readonly FakeLinkRepository _links = new();
    private readonly FakeUnitOfWork _unitOfWork = new();

    private Category AddCategory()
    {
        var category = Category.Create("Finance");
        _categories.Items.Add(category);
        return category;
    }

    [Fact]
    public async Task CreateLink_WithUnknownRole_ThrowsInvalidRoleAndSavesNothing()
    {
        var category = AddCategory();
        var handler = new CreateLinkHandler(_links, _categories, _unitOfWork);

        var exception = await Assert.ThrowsAsync<ValidationException>(() =>
            handler.HandleAsync(
                new CreateLinkCommand("Payroll", "https://payroll.local", category.Id, new[] { "Admn" }),
                CancellationToken.None));

        Assert.Contains("InvalidRole", exception.Errors);
        Assert.Empty(_links.Items);
        Assert.Equal(0, _unitOfWork.SaveCount);
    }

    [Fact]
    public async Task CreateLink_WithRolesInDifferentCase_StoresCanonicalNames()
    {
        var category = AddCategory();
        var handler = new CreateLinkHandler(_links, _categories, _unitOfWork);

        await handler.HandleAsync(
            new CreateLinkCommand("Payroll", "https://payroll.local", category.Id, new[] { "user", " ADMIN " }),
            CancellationToken.None);

        var roles = _links.Items.Single().RoleAccesses.Select(access => access.RoleName).OrderBy(r => r);

        Assert.Equal(new[] { "Admin", "User" }, roles);
    }

    [Fact]
    public async Task UpdateLink_WithUnknownRole_ThrowsInvalidRoleAndKeepsOldRoles()
    {
        var category = AddCategory();
        var link = Link.Create("Payroll", "https://payroll.local", category.Id, new[] { "User" });
        _links.Items.Add(link);
        var handler = new UpdateLinkHandler(_links, _categories, _unitOfWork);

        var exception = await Assert.ThrowsAsync<ValidationException>(() =>
            handler.HandleAsync(
                new UpdateLinkCommand(link.Id, "Payroll", "https://payroll.local", category.Id, new[] { "Foo" }),
                CancellationToken.None));

        Assert.Contains("InvalidRole", exception.Errors);
        Assert.Equal("User", Assert.Single(link.RoleAccesses).RoleName);
        Assert.Equal(0, _unitOfWork.SaveCount);
    }
}