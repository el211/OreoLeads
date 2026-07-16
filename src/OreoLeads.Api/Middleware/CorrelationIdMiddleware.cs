using Serilog.Context;

namespace OreoLeads.Api.Middleware;

/// <summary>
/// Adds X-Correlation-Id and X-Request-Id headers to every request and response.
/// Values are pushed into the Serilog log context so every log line carries the IDs.
/// </summary>
public class CorrelationIdMiddleware
{
    private const string CorrelationIdHeader = "X-Correlation-Id";
    private const string RequestIdHeader = "X-Request-Id";

    private readonly RequestDelegate _next;

    public CorrelationIdMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(HttpContext context)
    {
        // Accept an existing correlation ID from the caller, or generate one
        var correlationId = context.Request.Headers[CorrelationIdHeader].FirstOrDefault()
            ?? Guid.NewGuid().ToString("N");
        var requestId = context.TraceIdentifier;

        context.Response.Headers[CorrelationIdHeader] = correlationId;
        context.Response.Headers[RequestIdHeader] = requestId;

        using (LogContext.PushProperty("CorrelationId", correlationId))
        using (LogContext.PushProperty("RequestId", requestId))
        {
            await _next(context);
        }
    }
}
