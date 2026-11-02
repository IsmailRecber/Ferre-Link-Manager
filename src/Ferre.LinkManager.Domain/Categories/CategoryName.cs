using Ferre.LinkManager.Domain.Common;

namespace Ferre.LinkManager.Domain.Categories;

public sealed class CategoryName : ValueObject
{
    public const int MaxLength = 100;

    private CategoryName(string value)
    {
        Value = value;
    }

    public string Value { get; }

    public static CategoryName Create(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new DomainException("Category name cannot be empty.");
        }

        var trimmed = value.Trim();

        if (trimmed.Length > MaxLength)
        {
            throw new DomainException($"Category name cannot exceed {MaxLength} characters.");
        }

        return new CategoryName(trimmed);
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString() => Value;
}