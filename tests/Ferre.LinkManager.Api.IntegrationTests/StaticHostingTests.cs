using System.Net;
using Xunit;

namespace Ferre.LinkManager.Api.IntegrationTests;

public class StaticHostingTests : IClassFixture<TestApiFactory>
{
    private readonly TestApiFactory _factory;

    public StaticHostingTests(TestApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task UnknownApiAddress_AnswersNotFound_NotTheAngularPage()
    {
        var response = await _factory.CreateApiClient().GetAsync("/api/does-not-exist");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.NotEqual("text/html", response.Content.Headers.ContentType?.MediaType);
    }

    [Theory]
    [InlineData("/")]
    [InlineData("/api/auth/me")]
    public async Task Responses_CarryTheSecurityHeaders(string url)
    {
        var response = await _factory.CreateApiClient().GetAsync(url);

        Assert.Equal("nosniff", Assert.Single(response.Headers.GetValues("X-Content-Type-Options")));
        Assert.Equal("DENY", Assert.Single(response.Headers.GetValues("X-Frame-Options")));
        Assert.Equal("no-referrer", Assert.Single(response.Headers.GetValues("Referrer-Policy")));
        Assert.True(response.Headers.Contains("Permissions-Policy"));
        Assert.True(response.Headers.Contains("Content-Security-Policy-Report-Only"));
    }
}