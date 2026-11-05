using System.Security.Claims;
using Ferre.LinkManager.Api.Security;
using Ferre.LinkManager.Application.Users.ConfirmEmail;
using Ferre.LinkManager.Application.Users.Register;
using Ferre.LinkManager.Application.Users.ResendVerificationEmail;
using Ferre.LinkManager.Infrastructure.Identity;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;

namespace Ferre.LinkManager.Api.Features.Users;

public static class AuthEndpoints
{
    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        // The client calls this to get the XSRF-TOKEN cookie, then sends it back in the X-XSRF-TOKEN header.
        app.MapGet("/api/antiforgery", (HttpContext httpContext, IAntiforgery antiforgery) =>
        {
            var tokens = antiforgery.GetAndStoreTokens(httpContext);

            httpContext.Response.Cookies.Append("XSRF-TOKEN", tokens.RequestToken!, new CookieOptions
            {
                HttpOnly = false, // JavaScript must be able to read it
                Secure = true,
                SameSite = SameSiteMode.Strict,
                IsEssential = true
            });

            return Results.NoContent();
        });

        app.MapGet("/api/auth/me", (ClaimsPrincipal user) =>
        {
            var userId = Guid.TryParse(user.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : (Guid?)null;
            var roles = user.FindAll(ClaimTypes.Role).Select(claim => claim.Value).ToArray();

            return Results.Ok(new MeResponse(userId, user.FindFirstValue(ClaimTypes.Email), roles));
        }).RequireAuthorization();

        var auth = app.MapGroup("/api/auth").AddEndpointFilter<AntiforgeryEndpointFilter>();

        auth.MapPost("/register", async (
            RegisterRequest request, RegisterHandler handler, CancellationToken cancellationToken) =>
        {
            var response = await handler.HandleAsync(
                new RegisterCommand(request.Email, request.Password), cancellationToken);

            return Results.Ok(response);
        });

        auth.MapPost("/confirm-email", async (
            ConfirmEmailRequest request, ConfirmEmailHandler handler, CancellationToken cancellationToken) =>
        {
            await handler.HandleAsync(new ConfirmEmailCommand(request.UserId, request.Token), cancellationToken);

            return Results.NoContent();
        });

        auth.MapPost("/resend-verification", async (
            ResendVerificationEmailRequest request,
            ResendVerificationEmailHandler handler,
            CancellationToken cancellationToken) =>
        {
            await handler.HandleAsync(new ResendVerificationEmailCommand(request.UserId), cancellationToken);

            // Always the same answer, so nobody can learn whether an account exists.
            return Results.Accepted();
        });

        auth.MapPost("/login", async (
            LoginRequest request,
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager) =>
        {
            var email = request.Email?.Trim();

            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrEmpty(request.Password))
            {
                return Failure(StatusCodes.Status401Unauthorized, "InvalidCredentials");
            }

            var user = await userManager.FindByEmailAsync(email);

            if (user is null || !user.IsActive)
            {
                return Failure(StatusCodes.Status401Unauthorized, "InvalidCredentials");
            }

            var result = await signInManager.PasswordSignInAsync(
                user, request.Password, isPersistent: false, lockoutOnFailure: true);

            if (result.Succeeded)
            {
                return Results.NoContent();
            }

            if (result.IsLockedOut)
            {
                return Failure(StatusCodes.Status429TooManyRequests, "AccountLocked");
            }

            if (result.IsNotAllowed)
            {
                return Failure(StatusCodes.Status403Forbidden, "EmailNotConfirmed");
            }

            return Failure(StatusCodes.Status401Unauthorized, "InvalidCredentials");
        });

        auth.MapPost("/logout", async (HttpContext httpContext) =>
        {
            await httpContext.SignOutAsync(IdentityConstants.ApplicationScheme);

            return Results.NoContent();
        }).RequireAuthorization();

        return app;
    }

    private static IResult Failure(int statusCode, string code) =>
        Results.Problem(
            statusCode: statusCode,
            title: code,
            extensions: new Dictionary<string, object?> { ["errors"] = new[] { code } });
}