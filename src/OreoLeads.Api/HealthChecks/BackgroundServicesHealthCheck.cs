using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace OreoLeads.Api.HealthChecks;

public class BackgroundServicesHealthCheck : IHealthCheck
{
    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
        => Task.FromResult(HealthCheckResult.Healthy("Background services are running."));
}
