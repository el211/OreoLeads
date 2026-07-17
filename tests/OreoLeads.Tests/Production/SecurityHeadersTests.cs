using FluentAssertions;
using Microsoft.AspNetCore.Http;
using OreoLeads.Api.Middleware;

namespace OreoLeads.Tests.Production;

public class SecurityHeadersTests
{
    private readonly SecurityHeadersMiddleware _middleware;

    public SecurityHeadersTests()
    {
        _middleware = new SecurityHeadersMiddleware(_ => Task.CompletedTask);
    }

    [Fact]
    public async Task SecurityHeaders_XFrameOptions_IsDeny()
    {
        var ctx = new DefaultHttpContext();
        await _middleware.InvokeAsync(ctx);
        ctx.Response.Headers["X-Frame-Options"].ToString().Should().Be("DENY");
    }

    [Fact]
    public async Task SecurityHeaders_XContentTypeOptions_IsNosniff()
    {
        var ctx = new DefaultHttpContext();
        await _middleware.InvokeAsync(ctx);
        ctx.Response.Headers["X-Content-Type-Options"].ToString().Should().Be("nosniff");
    }

    [Fact]
    public async Task SecurityHeaders_XXssProtection_IsSet()
    {
        var ctx = new DefaultHttpContext();
        await _middleware.InvokeAsync(ctx);
        ctx.Response.Headers["X-XSS-Protection"].ToString().Should().Be("1; mode=block");
    }

    [Fact]
    public async Task SecurityHeaders_ReferrerPolicy_IsSet()
    {
        var ctx = new DefaultHttpContext();
        await _middleware.InvokeAsync(ctx);
        ctx.Response.Headers["Referrer-Policy"].ToString()
            .Should().Be("strict-origin-when-cross-origin");
    }
}
