using FluentAssertions;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using OreoLeads.Api.HealthChecks;

namespace OreoLeads.Tests.Production;

public class HealthCheckTests
{
    [Fact]
    public async Task AutomationHealthCheck_ReturnsHealthy()
    {
        var check = new AutomationHealthCheck();
        var result = await check.CheckHealthAsync(new HealthCheckContext());
        result.Status.Should().Be(HealthStatus.Healthy);
    }

    [Fact]
    public async Task BackgroundServicesHealthCheck_ReturnsHealthy()
    {
        var check = new BackgroundServicesHealthCheck();
        var result = await check.CheckHealthAsync(new HealthCheckContext());
        result.Status.Should().Be(HealthStatus.Healthy);
    }

    [Fact]
    public async Task AutomationHealthCheck_Description_IsNotEmpty()
    {
        var check = new AutomationHealthCheck();
        var result = await check.CheckHealthAsync(new HealthCheckContext());
        result.Description.Should().NotBeNullOrWhiteSpace();
    }
}
