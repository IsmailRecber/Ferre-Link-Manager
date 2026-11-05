using Ferre.LinkManager.Api.Security;
using Ferre.LinkManager.Application.Categories.CreateCategory;
using Ferre.LinkManager.Application.Categories.DeleteCategory;
using Ferre.LinkManager.Application.Categories.UpdateCategory;

namespace Ferre.LinkManager.Api.Features.Categories;

public static class CategoryEndpoints
{
    public static IEndpointRouteBuilder MapCategoryEndpoints(this IEndpointRouteBuilder app)
    {
        // Every endpoint in this group: Admin only + antiforgery token required.
        var group = app.MapGroup("/api/categories")
            .RequireAuthorization(AuthorizationPolicies.AdminOnly)
            .AddEndpointFilter<AntiforgeryEndpointFilter>();

        group.MapPost("/", async (
            CreateCategoryRequest request, CreateCategoryHandler handler, CancellationToken cancellationToken) =>
        {
            var response = await handler.HandleAsync(new CreateCategoryCommand(request.Name), cancellationToken);

            return Results.Ok(response);
        });

        group.MapPut("/{id:guid}", async (
            Guid id, UpdateCategoryRequest request, UpdateCategoryHandler handler, CancellationToken cancellationToken) =>
        {
            await handler.HandleAsync(new UpdateCategoryCommand(id, request.Name, request.IsActive), cancellationToken);

            return Results.NoContent();
        });

        group.MapDelete("/{id:guid}", async (
            Guid id, DeleteCategoryHandler handler, CancellationToken cancellationToken) =>
        {
            await handler.HandleAsync(new DeleteCategoryCommand(id), cancellationToken);

            return Results.NoContent();
        });

        return app;
    }
}