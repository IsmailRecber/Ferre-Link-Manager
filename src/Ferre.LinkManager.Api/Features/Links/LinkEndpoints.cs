using System.Security.Claims;
using Ferre.LinkManager.Api.Security;
using Ferre.LinkManager.Application.Links.CreateLink;
using Ferre.LinkManager.Application.Links.DeleteLink;
using Ferre.LinkManager.Application.Links.GetAllLinks;
using Ferre.LinkManager.Application.Links.GetLinkRedirect;
using Ferre.LinkManager.Application.Links.GetMyLinks;
using Ferre.LinkManager.Application.Links.UpdateLink;

namespace Ferre.LinkManager.Api.Features.Links;

public static class LinkEndpoints
{
    public static IEndpointRouteBuilder MapLinkEndpoints(this IEndpointRouteBuilder app)
    {
        // ---- Any signed-in user ----
        var user = app.MapGroup("/api/links").RequireAuthorization();

        user.MapGet("/my", async (
            ClaimsPrincipal principal, GetMyLinksHandler handler, CancellationToken cancellationToken) =>
        {
            var links = await handler.HandleAsync(new GetMyLinksQuery(GetRoles(principal)), cancellationToken);

            return Results.Ok(links);
        });

        // The destination is read from the database. The client only sends the link id.
        user.MapGet("/{id:guid}/go", async (
            Guid id,
            ClaimsPrincipal principal,
            GetLinkRedirectHandler handler,
            CancellationToken cancellationToken) =>
        {
            var url = await handler.HandleAsync(new GetLinkRedirectQuery(id, GetRoles(principal)), cancellationToken);

            return Results.Redirect(url, permanent: false);
        });

        // ---- Admin only ----
        var admin = app.MapGroup("/api/links").RequireAuthorization(AuthorizationPolicies.AdminOnly);

        admin.MapGet("/", async (GetAllLinksHandler handler, CancellationToken cancellationToken) =>
            Results.Ok(await handler.HandleAsync(cancellationToken)));

        var writes = admin.MapGroup("/").AddEndpointFilter<AntiforgeryEndpointFilter>();

        writes.MapPost("", async (
            CreateLinkRequest request, CreateLinkHandler handler, CancellationToken cancellationToken) =>
        {
            var response = await handler.HandleAsync(
                new CreateLinkCommand(
                    request.Title, request.Url, request.CategoryId, request.Roles ?? Array.Empty<string>()),
                cancellationToken);

            return Results.Ok(response);
        });

        writes.MapPut("{id:guid}", async (
            Guid id, UpdateLinkRequest request, UpdateLinkHandler handler, CancellationToken cancellationToken) =>
        {
            await handler.HandleAsync(
                new UpdateLinkCommand(
                    id, request.Title, request.Url, request.CategoryId, request.Roles ?? Array.Empty<string>()),
                cancellationToken);

            return Results.NoContent();
        });

        writes.MapDelete("{id:guid}", async (
            Guid id, DeleteLinkHandler handler, CancellationToken cancellationToken) =>
        {
            await handler.HandleAsync(new DeleteLinkCommand(id), cancellationToken);

            return Results.NoContent();
        });

        return app;
    }

    private static string[] GetRoles(ClaimsPrincipal principal) =>
        principal.FindAll(ClaimTypes.Role).Select(claim => claim.Value).ToArray();
}