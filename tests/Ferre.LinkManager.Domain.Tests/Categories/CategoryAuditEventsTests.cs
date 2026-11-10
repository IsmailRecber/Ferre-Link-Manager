using Ferre.LinkManager.Domain.Categories;
using Xunit;

namespace Ferre.LinkManager.Domain.Tests.Categories;

public class CategoryAuditEventsTests
{
    private static Category CreateCategory()
    {
        var category = Category.Create("Finance");

        // Forget the "created" event, so each test sees only the events it caused.
        category.ClearDomainEvents();

        return category;
    }

    [Fact]
    public void Rename_ToANewName_RaisesRenamedEventWithOldAndNewName()
    {
        var category = CreateCategory();

        category.Rename("Accounting");

        var domainEvent = Assert.IsType<CategoryRenamed>(Assert.Single(category.DomainEvents));
        Assert.Equal(category.Id, domainEvent.CategoryId);
        Assert.Equal("Finance", domainEvent.OldName);
        Assert.Equal("Accounting", domainEvent.NewName);
    }

    [Fact]
    public void Rename_ToTheSameName_RaisesNothing()
    {
        var category = CreateCategory();

        category.Rename("  Finance ");

        Assert.Empty(category.DomainEvents);
    }

    [Fact]
    public void Deactivate_ActiveCategory_RaisesDeactivatedEvent()
    {
        var category = CreateCategory();

        category.Deactivate();

        var domainEvent = Assert.IsType<CategoryDeactivated>(Assert.Single(category.DomainEvents));
        Assert.Equal("Finance", domainEvent.Name);
    }

    [Fact]
    public void Deactivate_Twice_RaisesOnlyOneEvent()
    {
        var category = CreateCategory();

        category.Deactivate();
        category.Deactivate();

        Assert.Single(category.DomainEvents);
    }

    [Fact]
    public void Activate_WhenAlreadyActive_RaisesNothing()
    {
        var category = CreateCategory();

        category.Activate();

        Assert.Empty(category.DomainEvents);
    }

    [Fact]
    public void Activate_AfterDeactivate_RaisesActivatedEvent()
    {
        var category = CreateCategory();
        category.Deactivate();
        category.ClearDomainEvents();

        category.Activate();

        Assert.IsType<CategoryActivated>(Assert.Single(category.DomainEvents));
    }
}