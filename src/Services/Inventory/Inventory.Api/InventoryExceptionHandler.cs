using System.Diagnostics;
using Inventory.Application;
using Inventory.Domain;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace Inventory.Api;

public sealed class InventoryExceptionHandler(IProblemDetailsService problems, ILogger<InventoryExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext context, Exception exception, CancellationToken cancellationToken)
    {
        if (exception is OperationCanceledException && context.RequestAborted.IsCancellationRequested) return false;

        var status = exception switch
        {
            InventoryValidationException or BadHttpRequestException => StatusCodes.Status400BadRequest,
            InventoryNotFoundException => StatusCodes.Status404NotFound,
            InventoryConflictException or StockConflictException => StatusCodes.Status409Conflict,
            _ => StatusCodes.Status500InternalServerError
        };
        ProblemDetails details = exception is InventoryValidationException validation
            ? new HttpValidationProblemDetails(new Dictionary<string, string[]> { [validation.Field] = [validation.Message] })
            : new ProblemDetails();
        details.Status = status;
        details.Title = status switch
        {
            400 => "Request validation failed.",
            404 => "Resource not found.",
            409 => "Inventory conflict.",
            _ => "An unexpected server error occurred."
        };
        details.Detail = exception is InventoryNotFoundException or InventoryConflictException or StockConflictException ? exception.Message : null;
        details.Instance = context.Request.Path;
        details.Extensions["traceId"] = Activity.Current?.Id ?? context.TraceIdentifier;
        if (status == 500) logger.LogError(exception, "Inventory request failed with trace {TraceId}", context.TraceIdentifier);
        context.Response.StatusCode = status;
        await problems.WriteAsync(new ProblemDetailsContext { HttpContext = context, ProblemDetails = details });
        return true;
    }
}
