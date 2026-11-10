using System.Net;
using Xunit;

namespace Ferre.LinkManager.Api.IntegrationTests;

public class ResendVerificationApiTests : IClassFixture<TestApiFactory>
{
    private readonly TestApiFactory _factory;

    public ResendVerificationApiTests(TestApiFactory factory)
    {
        _factory = factory;
    }

    // One test on purpose: the limit (5 requests per 10 minutes) is counted per client and per application
    // instance, so splitting the steps into several tests would make them depend on each other.
    [Fact]
    public async Task ResendByEmail_AnswersTheSameForUnknownAddresses_AndLimitsRequests()
    {
        var client = _factory.CreateApiClient();

        // 1) An address that has no account: same "accepted" answer, nothing is revealed.
        var unknown = await client.SendAsync(
            HttpMethod.Post, "/api/auth/resend-verification-by-email",
            new { email = $"nobody-{Guid.NewGuid()}@example.test" });
        Assert.Equal(HttpStatusCode.Accepted, unknown.StatusCode);

        // 2) A blank address is a malformed request.
        var blank = await client.SendAsync(
            HttpMethod.Post, "/api/auth/resend-verification-by-email", new { email = "  " });
        Assert.Equal(HttpStatusCode.BadRequest, blank.StatusCode);

        // 3) Requests 3, 4 and 5 are still accepted.
        for (var i = 0; i < 3; i++)
        {
            var response = await client.SendAsync(
                HttpMethod.Post, "/api/auth/resend-verification-by-email",
                new { email = $"nobody-{Guid.NewGuid()}@example.test" });
            Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
        }

        // 4) The sixth request inside the window is rejected.
        var limited = await client.SendAsync(
            HttpMethod.Post, "/api/auth/resend-verification-by-email",
            new { email = $"nobody-{Guid.NewGuid()}@example.test" });
        Assert.Equal(HttpStatusCode.TooManyRequests, limited.StatusCode);
    }
}