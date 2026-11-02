using Ferre.LinkManager.Domain.Categories;
using Ferre.LinkManager.Domain.Common;
using Xunit;

namespace Ferre.LinkManager.Domain.Tests.Categories;

public class CategoryNameTests
{
    [Fact]
    public void Create_WithValidValue_TrimsWhitespace()
    {
        var name = CategoryName.Create("  Finance  ");

        Assert.Equal("Finance", name.Value);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithEmptyValue_ThrowsDomainException(string? value)
    {
        Assert.Throws<DomainException>(() => CategoryName.Create(value));
    }

    [Fact]
    public void Create_LongerThanMaxLength_ThrowsDomainException()
    {
        var tooLong = new string('a', CategoryName.MaxLength + 1);

        Assert.Throws<DomainException>(() => CategoryName.Create(tooLong));
    }

    [Fact]
    public void Create_WithExactlyMaxLength_Succeeds()
    {
        var exact = new string('a', CategoryName.MaxLength);

        var name = CategoryName.Create(exact);

        Assert.Equal(CategoryName.MaxLength, name.Value.Length);
    }

    [Fact]
    public void TwoNamesWithSameValue_AreEqual()
    {
        var first = CategoryName.Create("Finance");
        var second = CategoryName.Create("Finance");

        Assert.Equal(first, second);
    }
}