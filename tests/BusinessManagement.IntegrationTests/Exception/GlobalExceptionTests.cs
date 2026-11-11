using BusinessManagement.Application.Common;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Npgsql;

public class GlobalExceptionHandlerTests
{
    [Fact]
    public async Task Handle_NotFoundException_Returns404()
    {
        var exception = new NotFoundException("Customer not found.");

        var (statusCode, body) = await InvokeAsync(exception);

        Assert.Equal(404, statusCode);
        Assert.Contains("Resource not found", body);
        Assert.Contains("Customer not found.", body);
    }

    [Fact]
    public async Task Handle_ConflictException_Returns409()
    {
        var exception = new ConflictException("Email already exists.");

        var (statusCode, body) = await InvokeAsync(exception);

        Assert.Equal(409, statusCode);
        Assert.Contains("Conflict", body);
        Assert.Contains("Email already exists.", body);
    }

    [Fact]
    public async Task Handle_UniqueViolation_Returns409WithoutConstraintName()
    {
        const string constraintName = "UQ_Customers_Email";

        var postgresException = new PostgresException(
            "duplicate key value violates unique constraint",
            "ERROR",
            "ERROR",
            "23505",
            constraintName: constraintName);

        var exception = new DbUpdateException(
            "An error occurred while saving changes.",
            postgresException);

        var (statusCode, body) = await InvokeAsync(exception);

        Assert.Equal(409, statusCode);
        Assert.Contains("Data conflict", body);
        Assert.Contains(
            "The request conflicts with existing data.",
            body);

        Assert.DoesNotContain(constraintName, body);
    }

    [Fact]
    public async Task Handle_UnknownException_Returns500WithoutErrorDetails()
    {
        const string secretMessage = "Sensitive database connection details";

        var exception = new Exception(secretMessage);

        var (statusCode, body) = await InvokeAsync(exception);

        Assert.Equal(500, statusCode);
        Assert.Contains("An unexpected error occurred", body);
        Assert.DoesNotContain(secretMessage, body);
    }

    private static async Task<(int StatusCode, string Body)> InvokeAsync(
        Exception exception)
    {
        var context = new DefaultHttpContext();

        context.Response.Body = new MemoryStream();

        var handler = new GlobalExceptionHandler(
            NullLogger<GlobalExceptionHandler>.Instance);

        await handler.TryHandleAsync(
            context,
            exception,
            CancellationToken.None);

        context.Response.Body.Position = 0;

        using var reader = new StreamReader(context.Response.Body);

        var body = await reader.ReadToEndAsync();

        return (context.Response.StatusCode, body);
    }
    [Theory]
    [InlineData("23001")]
    [InlineData("23503")]
    [InlineData("23505")]
    public async Task Handle_IntegrityViolation_Returns409(string sqlState)
    {
        var pg = new PostgresException("msg", "ERROR", "ERROR", sqlState);
        var (status, _) = await InvokeAsync(new DbUpdateException("save failed", pg));
        Assert.Equal(409, status);
    }

    [Fact]
    public async Task Handle_NonIntegrityDbError_Returns500WithoutDetails()   // 22001: chuỗi quá dài
    {
        var pg = new PostgresException("value too long", "ERROR", "ERROR", "22001");
        var (status, body) = await InvokeAsync(new DbUpdateException("save failed", pg));
        Assert.Equal(500, status);
        Assert.DoesNotContain("value too long", body);
    }
}