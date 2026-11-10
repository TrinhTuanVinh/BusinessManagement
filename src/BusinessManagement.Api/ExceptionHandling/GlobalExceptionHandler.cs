using BusinessManagement.Application.Common;
using BusinessManagement.Domain.Common;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Npgsql;

public sealed class GlobalExceptionHandler : IExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandler> _logger;
    public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger) => _logger = logger;
    public async ValueTask<bool> TryHandleAsync(HttpContext ctx, Exception ex, CancellationToken ct)
    {
        var (status, title, detail) = Classify(ex);

        if (status >= 500) _logger.LogError(ex, "Unhandled exception for {Path}", ctx.Request.Path);
        else if (ex is DbUpdateException) _logger.LogWarning(ex, "Data conflict for {Path}", ctx.Request.Path);   // tên constraint chỉ nằm trong log
        else _logger.LogInformation("Request {Path} rejected with {Status}", ctx.Request.Path, status);

        var problem = new ProblemDetails { Status = status, Title = title, Detail = detail };
        problem.Extensions["traceId"] = ctx.TraceIdentifier;
        ctx.Response.StatusCode = status;
        await ctx.Response.WriteAsJsonAsync(problem, ct);
        return true;
    }

    private static (int Status, string Title, string? Detail) Classify(Exception ex) => ex switch
    {
        DomainException => (StatusCodes.Status422UnprocessableEntity, "Business rule violated", ex.Message),
        NotFoundException => (StatusCodes.Status404NotFound, "Resource not found", ex.Message),
        ConflictException => (StatusCodes.Status409Conflict, "Conflict", ex.Message),
        DbUpdateException { InnerException: PostgresException pg }
            when pg.SqlState.StartsWith("23", StringComparison.Ordinal)
                           => (StatusCodes.Status409Conflict, "Data conflict", "The request conflicts with existing data."),
        _ => (StatusCodes.Status500InternalServerError, "An unexpected error occurred", null),
    };
}