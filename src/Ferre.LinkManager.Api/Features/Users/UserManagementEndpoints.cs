using Ferre.LinkManager.Api.Security;
using Ferre.LinkManager.Application.Users.GetUsers;
using Ferre.LinkManager.Application.Users.SetUserActive;

namespace Ferre.LinkManager.Api.Features.Users;

public sealed record SetUserActiveRequest(bool IsActive);

public static class UserManagementEndpoints
{
    public static IEndpointRouteBuilder MapUserManagementEndpoints(this IEndpointRouteBuilder app)
    {
        // Admin only.
        var group = app.MapGroup("/api/users").RequireAuthorization(AuthorizationPolicies.AdminOnly);

        group.MapGet("/", async (GetUsersHandler handler, CancellationToken cancellationToken) =>
            Results.Ok(await handler.HandleAsync(cancellationToken)));

        group.MapPut("/{id:guid}/active", async (
                Guid id,
                SetUserActiveRequest request,
                SetUserActiveHandler handler,
                CancellationToken cancellationToken) =>
        {
            await handler.HandleAsync(new SetUserActiveCommand(id, request.IsActive), cancellationToken);

            return Results.NoContent();
        })
            .AddEndpointFilter<AntiforgeryEndpointFilter>();

        return app;
    }
}