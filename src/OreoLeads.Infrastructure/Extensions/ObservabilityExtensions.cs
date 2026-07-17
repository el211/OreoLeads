using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using OpenTelemetry.Metrics;
using OreoLeads.Infrastructure.Configuration;

namespace OreoLeads.Infrastructure.Extensions;

public static class ObservabilityExtensions
{
    public static IServiceCollection AddOreoLeadsObservability(
        this IServiceCollection services, IConfiguration configuration)
    {
        var opts = configuration.GetSection(ObservabilityOptions.SectionName)
            .Get<ObservabilityOptions>() ?? new ObservabilityOptions();

        var resourceBuilder = ResourceBuilder.CreateDefault()
            .AddService(opts.ServiceName, serviceVersion: opts.ServiceVersion);

        if (opts.EnableTracing)
        {
            services.AddOpenTelemetry()
                .WithTracing(tracing =>
                {
                    tracing
                        .SetResourceBuilder(resourceBuilder)
                        .AddAspNetCoreInstrumentation(o =>
                        {
                            o.RecordException = true;
                            o.Filter = ctx => !ctx.Request.Path.StartsWithSegments("/health");
                        })
                        .AddHttpClientInstrumentation()
                        .AddSource("OreoLeads.*");

                    tracing.AddConsoleExporter();
                })
                .WithMetrics(metrics =>
                {
                    metrics
                        .SetResourceBuilder(resourceBuilder)
                        .AddAspNetCoreInstrumentation()
                        .AddHttpClientInstrumentation();
                });
        }

        return services;
    }
}
