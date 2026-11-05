using Ferre.LinkManager.Api.Security;
using Ferre.LinkManager.Application.Links.CreateLink;
using Ferre.LinkManager.Application.Links.DeleteLink;
using Ferre.LinkManager.Application.Links.UpdateLink;

namespace Ferre.LinkManager.Api.Features.Links;

public static class LinkEndpoints
{
    public static IEndpointRouteBuilder MapLinkEndpoints(this IEndpointRouteBuilder app)
    {
        // Every endpoint in this group: Admin only + antiforgery token required.
        var group = app.MapGroup("/api/links")
            .RequireAuthorization(AuthorizationPolicies.AdminOnly)
            .AddEndpointFilter<AntiforgeryEndpointFilter>();

        group.MapPost("/", async (
            CreateLinkRequest request, CreateLinkHandler handler, CancellationToken cancellationToken) =>
        {
            var response = await handler.HandleAsync(
                new CreateLinkCommand(
                    request.Title, request.Url, request.CategoryId, request.Roles ?? Array.Empty<string>()),
                cancellationToken);

            return Results.Ok(response);
        });

        group.MapPut("/{id:guid}", async (
            Guid id, UpdateLinkRequest request, UpdateLinkHandler handler, CancellationToken cancellationToken) =>
        {
            await handler.HandleAsync(
                new UpdateLinkCommand(
                    id, request.Title, request.Url, request.CategoryId, request.Roles ?? Array.Empty<string>()),
                cancellationToken);

            return Results.NoContent();
        });

        group.MapDelete("/{id:guid}", async (
            Guid id, DeleteLinkHandler handler, CancellationToken cancellationToken) =>
        {
            await handler.HandleAsync(new DeleteLinkCommand(id), cancellationToken);

            return Results.NoContent();
        });

        return app;
    }
}