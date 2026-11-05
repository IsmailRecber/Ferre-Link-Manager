using System.Net.Http.Json;

namespace Ferre.LinkManager.Api.IntegrationTests;

/// <summary>
/// A tiny browser stand-in: it keeps cookies and sends the antiforgery token header like the real client will.
/// </summary>
internal sealed class ApiClient
{
    private readonly HttpClient _http;
    private readonly Dictionary<string, string> _cookies = new();

    public ApiClient(HttpClient http)
    {
        _http = http;
    }

    public Task<HttpResponseMessage> GetAsync(string url) =>
        SendRawAsync(new HttpRequestMessage(HttpMethod.Get, url));

    public async Task<HttpResponseMessage> SendAsync(
        HttpMethod method, string url, object? body = null, bool withAntiforgery = true)
    {
        string? token = null;

        if (withAntiforgery)
        {
            // The token is tied to the current user, so it is fetched again before every request.
            (await GetAsync("/api/antiforgery")).Dispose();
            token = Uri.UnescapeDataString(_cookies["XSRF-TOKEN"]);
        }

        var request = new HttpRequestMessage(method, url);

        if (body is not null)
        {
            request.Content = JsonContent.Create(body);
        }

        if (token is not null)
        {
            request.Headers.Add("X-XSRF-TOKEN", token);
        }

        return await SendRawAsync(request);
    }

    private async Task<HttpResponseMessage> SendRawAsync(HttpRequestMessage request)
    {
        if (_cookies.Count > 0)
        {
            request.Headers.Add("Cookie", string.Join("; ", _cookies.Select(c => $"{c.Key}={c.Value}")));
        }

        var response = await _http.SendAsync(request);

        if (response.Headers.TryGetValues("Set-Cookie", out var setCookies))
        {
            foreach (var header in setCookies)
            {
                var pair = header.Split(';')[0];
                var separator = pair.IndexOf('=');
                var name = pair[..separator];
                var value = pair[(separator + 1)..];
                var expired = header.Contains("expires=Thu, 01 Jan 1970", StringComparison.OrdinalIgnoreCase);

                if (string.IsNullOrEmpty(value) || expired)
                {
                    _cookies.Remove(name);
                }
                else
                {
                    _cookies[name] = value;
                }
            }
        }

        return response;
    }
}