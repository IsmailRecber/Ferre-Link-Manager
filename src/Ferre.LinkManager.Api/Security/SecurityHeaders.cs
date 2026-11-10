namespace Ferre.LinkManager.Api.Security;

public static class SecurityHeaders
{
    // Report-Only: the browser lists what the policy WOULD block in its console, but blocks nothing.
    // After it has been watched without violations, the header name below can be changed to
    // "Content-Security-Policy" to enforce the policy.
    private const string PolicyHeaderName = "Content-Security-Policy-Report-Only";

    private const string Policy =
        "default-src 'self'; " +
        "script-src 'self'; " +
        "style-src 'self' 'unsafe-inline' https://fonts.googleapis.com; " +
        "font-src 'self' https://fonts.gstatic.com data:; " +
        "img-src 'self' data:; " +
        "connect-src 'self'; " +
        "object-src 'none'; " +
        "base-uri 'self'; " +
        "form-action 'self'; " +
        "frame-ancestors 'none'";

    /// <summary>Adds the browser security headers to every response.</summary>
    public static IApplicationBuilder UseSecurityHeaders(this IApplicationBuilder app) =>
        app.Use(async (context, next) =>
        {
            var headers = context.Response.Headers;

            headers["X-Content-Type-Options"] = "nosniff";
            headers["X-Frame-Options"] = "DENY";
            headers["Referrer-Policy"] = "no-referrer";
            headers["Permissions-Policy"] = "camera=(), microphone=(), geolocation=()";
            headers[PolicyHeaderName] = Policy;

            await next();
        });
}