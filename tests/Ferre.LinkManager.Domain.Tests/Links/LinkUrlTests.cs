using Ferre.LinkManager.Domain.Common;
using Ferre.LinkManager.Domain.Links;
using Xunit;

namespace Ferre.LinkManager.Domain.Tests.Links;

public class LinkUrlTests
{
    [Theory]
    [InlineData("http://intranet.local")]
    [InlineData("https://example.com/path?x=1")]
    [InlineData("HTTPS://EXAMPLE.COM")]
    public void Create_WithHttpOrHttps_Succeeds(string value)
    {
        var url = LinkUrl.Create(value);

        Assert.Equal(value, url.Value);
    }

    [Fact]
    public void Create_TrimsWhitespace()
    {
        var url = LinkUrl.Create("  https://example.com  ");

        Assert.Equal("https://example.com", url.Value);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithEmptyValue_Throws(string? value)
    {
        Assert.Throws<DomainException>(() => LinkUrl.Create(value));
    }

    [Theory]
    [InlineData("file:///C:/secret.txt")]
    [InlineData(@"\\server\share")]
    [InlineData("javascript:alert(1)")]
    [InlineData("ftp://example.com")]
    [InlineData("mailto:a@b.com")]
    public void Create_WithUnsupportedScheme_Throws(string value)
    {
        Assert.Throws<DomainException>(() => LinkUrl.Create(value));
    }

    [Theory]
    [InlineData("example.com")]
    [InlineData("/relative/path")]
    [InlineData("not a url")]
    public void Create_WithNonAbsoluteUrl_Throws(string value)
    {
        Assert.Throws<DomainException>(() => LinkUrl.Create(value));
    }

    [Fact]
    public void Create_WithCredentialsInUrl_Throws()
    {
        Assert.Throws<DomainException>(() => LinkUrl.Create("https://user:pass@example.com"));
    }

    [Fact]
    public void Create_LongerThanMaxLength_Throws()
    {
        var tooLong = "https://example.com/" + new string('a', LinkUrl.MaxLength);

        Assert.Throws<DomainException>(() => LinkUrl.Create(tooLong));
    }

    [Fact]
    public void TwoUrlsWithSameValue_AreEqual()
    {
        Assert.Equal(LinkUrl.Create("https://example.com"), LinkUrl.Create("https://example.com"));
    }
}