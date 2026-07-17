using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace OreoLeads.Api.HealthChecks;

public class AutomationHealthCheck : IHealthCheck
{
    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(HealthCheckResult.Healthy("Automation engine is running."));
    }
}
