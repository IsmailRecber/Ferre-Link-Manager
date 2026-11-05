using Microsoft.AspNetCore.Antiforgery;

namespace Ferre.LinkManager.Api.Security;

/// <summary>
/// Rejects state-changing requests that do not carry a valid antiforgery token header.
/// </summary>
public sealed class AntiforgeryEndpointFilter : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var antiforgery = context.HttpContext.RequestServices.GetRequiredService<IAntiforgery>();

        try
        {
            await antiforgery.ValidateRequestAsync(context.HttpContext);
        }
        catch (AntiforgeryValidationException)
        {
            return Results.Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "InvalidAntiforgeryToken",
                extensions: new Dictionary<string, object?> { ["errors"] = new[] { "InvalidAntiforgeryToken" } });
        }

        return await next(context);
    }
}