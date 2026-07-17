using FluentAssertions;
using Microsoft.AspNetCore.Http;
using OreoLeads.Api.Middleware;

namespace OreoLeads.Tests.Production;

public class CorrelationIdTests
{
    [Fact]
    public async Task CorrelationId_NotProvided_IsGenerated()
    {
        var middleware = new CorrelationIdMiddleware(_ => Task.CompletedTask);
        var ctx = new DefaultHttpContext();

        await middleware.InvokeAsync(ctx);

        ctx.Response.Headers["X-Correlation-Id"].ToString().Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task CorrelationId_Provided_IsPreserved()
    {
        var middleware = new CorrelationIdMiddleware(_ => Task.CompletedTask);
        var ctx = new DefaultHttpContext();
        ctx.Request.Headers["X-Correlation-Id"] = "test-correlation-id";

        await middleware.InvokeAsync(ctx);

        ctx.Response.Headers["X-Correlation-Id"].ToString().Should().Be("test-correlation-id");
    }

    [Fact]
    public async Task CorrelationId_RequestId_IsAddedToResponse()
    {
        var middleware = new CorrelationIdMiddleware(_ => Task.CompletedTask);
        var ctx = new DefaultHttpContext();

        await middleware.InvokeAsync(ctx);

        ctx.Response.Headers["X-Request-Id"].ToString().Should().NotBeNullOrWhiteSpace();
    }
}
