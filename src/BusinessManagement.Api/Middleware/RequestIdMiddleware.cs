using System.Text.RegularExpressions;

public sealed partial class RequestIdMiddleware(RequestDelegate next, ILogger<RequestIdMiddleware> logger)
{
    public const string HeaderName = "X-Request-Id";

    [GeneratedRegex("^[A-Za-z0-9_-]{8,64}$")]
    private static partial Regex ValidId();

    public async Task InvokeAsync(HttpContext ctx)
    {
        var incoming = ctx.Request.Headers[HeaderName].FirstOrDefault();
        var requestId = incoming is not null && ValidId().IsMatch(incoming)
            ? incoming
            : Guid.NewGuid().ToString("N");

        ctx.TraceIdentifier = requestId;
        ctx.Response.Headers[HeaderName] = requestId;

        using (logger.BeginScope(new Dictionary<string, object> { ["RequestId"] = requestId }))
            await next(ctx);
    }
}