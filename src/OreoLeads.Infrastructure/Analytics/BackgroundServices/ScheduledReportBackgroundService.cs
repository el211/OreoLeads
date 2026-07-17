using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OreoLeads.Domain.Enums;
using OreoLeads.Infrastructure.Persistence;

namespace OreoLeads.Infrastructure.Analytics.BackgroundServices;

internal sealed class ScheduledReportBackgroundService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<ScheduledReportBackgroundService> _logger;

    public ScheduledReportBackgroundService(IServiceScopeFactory scopeFactory, ILogger<ScheduledReportBackgroundService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessScheduledReportsAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing scheduled reports");
            }

            await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
        }
    }

    private async Task ProcessScheduledReportsAsync(CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var now = DateTime.UtcNow;
        var dueReports = await db.AnalyticsScheduledReports
            .Where(r => r.IsEnabled && r.NextSendAt.HasValue && r.NextSendAt <= now)
            .ToListAsync(ct);

        foreach (var report in dueReports)
        {
            _logger.LogInformation("Processing scheduled report '{Name}' (type={Type}, frequency={Frequency})",
                report.Name, report.ReportType, report.Frequency);

            // In production, this would generate the report and send via Brevo
            report.LastSentAt = now;
            report.NextSendAt = ReportService.ComputeNextSendAt(report.Frequency);
            report.SetUpdatedAt();

            _logger.LogInformation("Scheduled report '{Name}' processed. Next send at {NextSendAt}",
                report.Name, report.NextSendAt);
        }

        if (dueReports.Count > 0)
            await db.SaveChangesAsync(ct);
    }
}
