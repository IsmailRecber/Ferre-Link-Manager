using Ferre.LinkManager.Application.Common.Exceptions;
using Ferre.LinkManager.Domain.Common;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace Ferre.LinkManager.Api.Errors;

/// <summary>
/// Converts known application exceptions into HTTP responses. Unknown exceptions are left to the
/// default handler, which returns a generic 500 without leaking details.
/// </summary>
public sealed class GlobalExceptionHandler : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        int status;
        string title;
        string[] errors;

        switch (exception)
        {
            case ValidationException validation:
                status = StatusCodes.Status400BadRequest;
                title = "ValidationFailed";
                errors = validation.Errors.ToArray();
                break;
            case DomainException domain:
                status = StatusCodes.Status400BadRequest;
                title = domain.Message;
                errors = new[] { domain.Message };
                break;
            case NotFoundException notFound:
                status = StatusCodes.Status404NotFound;
                title = notFound.Message;
                errors = new[] { notFound.Message };
                break;
            case ConflictException conflict:
                status = StatusCodes.Status409Conflict;
                title = conflict.Message;
                errors = new[] { conflict.Message };
                break;
            default:
                return false;
        }

        var problem = new ProblemDetails { Status = status, Title = title };
        problem.Extensions["errors"] = errors;

        httpContext.Response.StatusCode = status;
        await httpContext.Response.WriteAsJsonAsync(
            problem, options: null, contentType: "application/problem+json", cancellationToken);

        return true;
    }
}