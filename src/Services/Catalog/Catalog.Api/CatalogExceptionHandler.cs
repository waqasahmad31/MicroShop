using System.Diagnostics;
using Catalog.Application;
using Catalog.Domain;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace Catalog.Api;

public sealed class CatalogExceptionHandler(IProblemDetailsService problems, ILogger<CatalogExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext context, Exception exception, CancellationToken cancellationToken)
    {
        if (exception is OperationCanceledException && context.RequestAborted.IsCancellationRequested) return false;

        var status = exception switch
        {
            CatalogValidationException or BadHttpRequestException => StatusCodes.Status400BadRequest,
            CatalogNotFoundException => StatusCodes.Status404NotFound,
            CatalogConflictException => StatusCodes.Status409Conflict,
            _ => StatusCodes.Status500InternalServerError
        };
        ProblemDetails details = exception is CatalogValidationException validation
            ? new HttpValidationProblemDetails(new Dictionary<string, string[]> { [validation.Field] = [validation.Message] })
            : new ProblemDetails();
        details.Status = status;
        details.Title = status switch
        {
            400 => "Request validation failed.",
            404 => "Resource not found.",
            409 => "Catalog conflict.",
            _ => "An unexpected server error occurred."
        };
        details.Detail = exception is CatalogNotFoundException or CatalogConflictException ? exception.Message : null;
        details.Instance = context.Request.Path;
        details.Extensions["traceId"] = Activity.Current?.Id ?? context.TraceIdentifier;
        if (status == 500) logger.LogError(exception, "Catalog request failed with trace {TraceId}", context.TraceIdentifier);
        context.Response.StatusCode = status;
        await problems.WriteAsync(new ProblemDetailsContext { HttpContext = context, ProblemDetails = details });
        return true;
    }
}
