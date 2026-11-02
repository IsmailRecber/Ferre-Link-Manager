using Ferre.LinkManager.Domain.Categories;
using Ferre.LinkManager.Domain.Common;
using Xunit;

namespace Ferre.LinkManager.Domain.Tests.Categories;

public class CategoryTests
{
    [Fact]
    public void Create_WithValidName_StartsActiveAndRaisesCreatedEvent()
    {
        var category = Category.Create("Finance");

        Assert.NotEqual(Guid.Empty, category.Id);
        Assert.Equal("Finance", category.Name.Value);
        Assert.True(category.IsActive);
        Assert.False(category.IsDeleted);

        var domainEvent = Assert.IsType<CategoryCreated>(Assert.Single(category.DomainEvents));
        Assert.Equal(category.Id, domainEvent.CategoryId);
    }

    [Fact]
    public void Create_WithInvalidName_ThrowsDomainException()
    {
        Assert.Throws<DomainException>(() => Category.Create("   "));
    }

    [Fact]
    public void Rename_WithValidName_ChangesName()
    {
        var category = Category.Create("Finance");

        category.Rename("Human Resources");

        Assert.Equal("Human Resources", category.Name.Value);
    }

    [Fact]
    public void Deactivate_SetsIsActiveToFalse()
    {
        var category = Category.Create("Finance");

        category.Deactivate();

        Assert.False(category.IsActive);
    }

    [Fact]
    public void Delete_MarksAsDeletedAndRaisesDeletedEvent()
    {
        var category = Category.Create("Finance");

        category.Delete();

        Assert.True(category.IsDeleted);
        Assert.Equal(2, category.DomainEvents.Count);
        Assert.Contains(category.DomainEvents, e => e is CategoryDeleted);
    }

    [Fact]
    public void Delete_WhenAlreadyDeleted_ThrowsDomainException()
    {
        var category = Category.Create("Finance");
        category.Delete();

        Assert.Throws<DomainException>(() => category.Delete());
    }

    [Fact]
    public void Rename_WhenDeleted_ThrowsDomainException()
    {
        var category = Category.Create("Finance");
        category.Delete();

        Assert.Throws<DomainException>(() => category.Rename("Other"));
    }
}