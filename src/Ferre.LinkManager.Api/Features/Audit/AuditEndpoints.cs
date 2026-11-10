using Ferre.LinkManager.Api.Security;
using Ferre.LinkManager.Application.Audit.GetAuditLogs;
using Microsoft.AspNetCore.Mvc;

namespace Ferre.LinkManager.Api.Features.Audit;

public static class AuditEndpoints
{
    public static IEndpointRouteBuilder MapAuditEndpoints(this IEndpointRouteBuilder app)
    {
        // Admin only. It only reads, so no antiforgery token is needed.
        var group = app.MapGroup("/api/audit-logs").RequireAuthorization(AuthorizationPolicies.AdminOnly);

        group.MapGet("/", async (
            int? page,
            int? pageSize,
            string? action,
            [FromQuery(Name = "from")] DateTimeOffset? fromDate,
            [FromQuery(Name = "to")] DateTimeOffset? toDate,
            GetAuditLogsHandler handler,
            CancellationToken cancellationToken) =>
        {
            var result = await handler.HandleAsync(
                new GetAuditLogsQuery(page, pageSize, action, fromDate?.UtcDateTime, toDate?.UtcDateTime),
                cancellationToken);

            return Results.Ok(result);
        });

        return app;
    }
}