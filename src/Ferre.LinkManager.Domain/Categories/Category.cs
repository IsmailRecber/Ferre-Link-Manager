using Ferre.LinkManager.Domain.Common;

namespace Ferre.LinkManager.Domain.Categories;

public sealed class Category : AggregateRoot
{
    private Category(Guid id, CategoryName name) : base(id)
    {
        Name = name;
        IsActive = true;
        CreatedOnUtc = DateTime.UtcNow;
    }

    public CategoryName Name { get; private set; }
    public bool IsActive { get; private set; }
    public bool IsDeleted { get; private set; }
    public DateTime CreatedOnUtc { get; }

    public static Category Create(string? name)
    {
        var category = new Category(Guid.NewGuid(), CategoryName.Create(name));
        category.RaiseDomainEvent(new CategoryCreated(category.Id, category.Name.Value));
        return category;
    }

    public void Rename(string? name)
    {
        EnsureNotDeleted();

        var newName = CategoryName.Create(name);

        // Nothing changed, so nothing is recorded.
        if (newName == Name)
        {
            return;
        }

        var oldName = Name;
        Name = newName;

        RaiseDomainEvent(new CategoryRenamed(Id, oldName.Value, newName.Value));
    }

    public void Activate()
    {
        EnsureNotDeleted();

        if (IsActive)
        {
            return;
        }

        IsActive = true;
        RaiseDomainEvent(new CategoryActivated(Id, Name.Value));
    }

    public void Deactivate()
    {
        EnsureNotDeleted();

        if (!IsActive)
        {
            return;
        }

        IsActive = false;
        RaiseDomainEvent(new CategoryDeactivated(Id, Name.Value));
    }

    public void Delete()
    {
        EnsureNotDeleted();
        IsDeleted = true;
        RaiseDomainEvent(new CategoryDeleted(Id, Name.Value));
    }

    private void EnsureNotDeleted()
    {
        if (IsDeleted)
        {
            throw new DomainException("Category is deleted.");
        }
    }
}