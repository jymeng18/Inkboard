using System.Data.Common;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Inkboard.API.Exceptions;

public class DbExceptionHandler : IExceptionHandler
{
    private readonly ILogger<DbExceptionHandler> _logger;

    public DbExceptionHandler(ILogger<DbExceptionHandler> logger)
    {
        _logger = logger;
    }

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken
    )
    {
        PostgresException? pgEx = exception switch
        {
            DbUpdateException dbUpdateEx => dbUpdateEx.InnerException as PostgresException,
            PostgresException directPgEx => directPgEx,
            _ => null,
        };

        if (pgEx is null)
        {
            return false;
        }

        if (pgEx.SqlState == PostgresErrorCodes.UniqueViolation) // 23505
        {
            _logger.LogWarning(
                exception,
                "Uniqueness constraint violated. Table: {Table}, Constraint: {Constraint}",
                pgEx.TableName,
                pgEx.ConstraintName
            );

            var problemDetails = new ProblemDetails
            {
                Status = StatusCodes.Status409Conflict,
                Title = "Conflict",
                Detail = "A resource with this unique value already exists.",
            };
            httpContext.Response.StatusCode = problemDetails.Status.Value;
            await httpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken);
            return true;
        }

        if (pgEx.SqlState == PostgresErrorCodes.CheckViolation) // 23514
        {
            _logger.LogWarning(
                exception,
                "Check constraint violated. Table: {Table}, Constraint: {Constraint}, Col: {Column}",
                pgEx.TableName,
                pgEx.ConstraintName,
                pgEx.ColumnName
            );

            string checkDetail = pgEx.ConstraintName switch
            {
                "CK_Friendships_UserOrder" => "UUID1 must be less then UUID2",
                _ => "Provided data violates a validation rule."
            };

            var problemDetails = new ProblemDetails
            {
                Status = StatusCodes.Status422UnprocessableEntity,
                Title = "Bad Request",
                Detail = checkDetail
            };
            httpContext.Response.StatusCode = problemDetails.Status.Value;
            await httpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken);
            return true;
        }

        // unexpected db errors
        _logger.LogError(
            exception,
            "Unexpected DB error: SqlState: {State}, Message: {Msg}",
            pgEx.SqlState,
            pgEx.Message
        );

        return false;
    }
}
