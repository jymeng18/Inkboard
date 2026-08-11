using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace Inkboard.API.Exceptions;

public class TimedOutExceptionHandler : IExceptionHandler
{
    private readonly ILogger<TimedOutExceptionHandler> _logger;

    public TimedOutExceptionHandler(ILogger<TimedOutExceptionHandler> logger)
    {
        _logger = logger;
    }

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken
    )
    {
        if (exception is not TimeoutException)
        {
            return false;
        }

        _logger.LogWarning(exception, "Request timed out: {ReqPath}", httpContext.Request.Path);

        var problemDetails = new ProblemDetails
        {
            Status = StatusCodes.Status504GatewayTimeout,
            Title = "Gateway Timeout",
            Detail = "Operation timed out."
        };

        httpContext.Response.StatusCode = problemDetails.Status.Value;
        await httpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken);
        return true;
    }
}
