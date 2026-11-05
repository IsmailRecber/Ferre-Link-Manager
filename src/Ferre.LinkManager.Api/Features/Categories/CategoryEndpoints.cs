using Ferre.LinkManager.Api.Security;
using Ferre.LinkManager.Application.Categories.CreateCategory;
using Ferre.LinkManager.Application.Categories.DeleteCategory;
using Ferre.LinkManager.Application.Categories.GetCategories;
using Ferre.LinkManager.Application.Categories.UpdateCategory;

namespace Ferre.LinkManager.Api.Features.Categories;

public static class CategoryEndpoints
{
    public static IEndpointRouteBuilder MapCategoryEndpoints(this IEndpointRouteBuilder app)
    {
        // Every endpoint in this group: Admin only + antiforgery token required (for state-changing requests).
        var group = app.MapGroup("/api/categories")
            .RequireAuthorization(AuthorizationPolicies.AdminOnly);

        group.MapGet("/", async (GetCategoriesHandler handler, CancellationToken cancellationToken) =>
            Results.Ok(await handler.HandleAsync(cancellationToken)));

        var writes = group.MapGroup("/").AddEndpointFilter<AntiforgeryEndpointFilter>();

        writes.MapPost("", async (
            CreateCategoryRequest request, CreateCategoryHandler handler, CancellationToken cancellationToken) =>
        {
            var response = await handler.HandleAsync(new CreateCategoryCommand(request.Name), cancellationToken);

            return Results.Ok(response);
        });

        writes.MapPut("{id:guid}", async (
            Guid id, UpdateCategoryRequest request, UpdateCategoryHandler handler, CancellationToken cancellationToken) =>
        {
            await handler.HandleAsync(new UpdateCategoryCommand(id, request.Name, request.IsActive), cancellationToken);

            return Results.NoContent();
        });

        writes.MapDelete("{id:guid}", async (
            Guid id, DeleteCategoryHandler handler, CancellationToken cancellationToken) =>
        {
            await handler.HandleAsync(new DeleteCategoryCommand(id), cancellationToken);

            return Results.NoContent();
        });

        return app;
    }
}