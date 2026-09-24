using System.Diagnostics;
using Ordering.Application;
using Ordering.Domain;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace Ordering.Api;

public sealed class OrderingExceptionHandler(IProblemDetailsService problems, ILogger<OrderingExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext context, Exception exception, CancellationToken cancellationToken)
    {
        if (exception is OperationCanceledException && context.RequestAborted.IsCancellationRequested) return false;

        var status = exception switch
        {
            OrderingValidationException or BadHttpRequestException => StatusCodes.Status400BadRequest,
            OrderingNotFoundException => StatusCodes.Status404NotFound,
            OrderingConflictException or OrderTransitionException => StatusCodes.Status409Conflict,
            OrderingDependencyException { Failure: DependencyFailure.Timeout } => StatusCodes.Status504GatewayTimeout,
            OrderingDependencyException { Failure: DependencyFailure.InvalidResponse } => StatusCodes.Status502BadGateway,
            OrderingDependencyException => StatusCodes.Status503ServiceUnavailable,
            _ => StatusCodes.Status500InternalServerError
        };
        ProblemDetails details = exception is OrderingValidationException validation
            ? new HttpValidationProblemDetails(new Dictionary<string, string[]> { [validation.Field] = [validation.Message] })
            : new ProblemDetails();
        details.Status = status;
        details.Title = status switch
        {
            400 => "Request validation failed.",
            404 => "Resource not found.",
            409 => "Ordering conflict.",
            502 => "A checkout dependency returned an invalid response.",
            503 => "A checkout dependency is unavailable.",
            504 => "Checkout timed out.",
            _ => "An unexpected server error occurred."
        };
        details.Detail = exception is OrderingNotFoundException or OrderingConflictException or OrderTransitionException ? exception.Message : null;
        details.Instance = context.Request.Path;
        details.Extensions["traceId"] = Activity.Current?.Id ?? context.TraceIdentifier;
        if (status == 500) logger.LogError(exception, "Ordering request failed with trace {TraceId}", context.TraceIdentifier);
        context.Response.StatusCode = status;
        await problems.WriteAsync(new ProblemDetailsContext { HttpContext = context, ProblemDetails = details });
        return true;
    }
}
