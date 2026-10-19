using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.Extensions.DependencyInjection;
using OreoLeads.Api.Controllers;

namespace OreoLeads.Tests.Prospecting;

public class ProspectingRoutingTests
{
    [Theory]
    [InlineData("GET", "campaigns", "List")]
    [InlineData("POST", "campaigns", "Create")]
    [InlineData("GET", "campaigns/11111111-1111-1111-1111-111111111111", "Detail")]
    [InlineData("POST", "campaigns/11111111-1111-1111-1111-111111111111/resume", "Transition")]
    [InlineData("POST", "campaigns/11111111-1111-1111-1111-111111111111/start", "Transition")]
    [InlineData("POST", "campaigns/11111111-1111-1111-1111-111111111111/pause", "Transition")]
    [InlineData("POST", "campaigns/11111111-1111-1111-1111-111111111111/cancel", "Transition")]
    public async Task PublicCampaignUrls_ReachTheExpectedAction(string method, string path, string action)
    {
        var builder = WebApplication.CreateBuilder();
        builder.Services.AddControllers().AddApplicationPart(typeof(ProspectingController).Assembly);
        await using var host = builder.Build();
        var pipeline = new ApplicationBuilder(host.Services);
        pipeline.UseRouting();
        ControllerActionDescriptor? matched = null;
        pipeline.Use(_ => context =>
        {
            matched = context.GetEndpoint()?.Metadata.GetMetadata<ControllerActionDescriptor>();
            return Task.CompletedTask;
        });
        pipeline.UseEndpoints(endpoints => endpoints.MapControllers());
        var context = new DefaultHttpContext { RequestServices = host.Services };
        context.Request.Method = method;
        context.Request.Path = "/api/prospecting/" + path;
        await pipeline.Build()(context);
        Assert.NotNull(matched);
        Assert.Equal(action, matched.ActionName);
    }
}
