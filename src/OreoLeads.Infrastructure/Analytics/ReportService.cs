using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OreoLeads.Application.Common.Interfaces;
using OreoLeads.Application.Features.Analytics.DTOs;
using OreoLeads.Domain.Entities.Analytics;
using OreoLeads.Domain.Enums;
using OreoLeads.Infrastructure.Persistence;

namespace OreoLeads.Infrastructure.Analytics;

internal sealed class ReportService : IReportService
{
    private readonly ApplicationDbContext _db;
    private readonly ILogger<ReportService> _logger;

    public ReportService(ApplicationDbContext db, ILogger<ReportService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<AnalyticsReport> CreateReportAsync(CreateReportDto dto, Guid? orgId, CancellationToken ct = default)
    {
        var report = new AnalyticsReport
        {
            Name = dto.Name,
            Description = dto.Description,
            ReportType = dto.ReportType,
            FilterJson = dto.FilterJson,
            Format = dto.Format,
            OrganizationId = orgId,
            Status = ReportStatus.Completed,
            GeneratedAt = DateTime.UtcNow
        };

        _db.AnalyticsReports.Add(report);
        await _db.SaveChangesAsync(ct);
        return report;
    }

    public async Task<List<AnalyticsReport>> GetReportsAsync(Guid? orgId, CancellationToken ct = default)
    {
        return await _db.AnalyticsReports
            .Where(r => r.OrganizationId == orgId)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync(ct);
    }

    public async Task<byte[]> ExportAsync(ExportRequestDto dto, Guid? orgId, CancellationToken ct = default)
    {
        var range = new DateRangeDto(dto.Preset, dto.From, dto.To);
        var (start, end) = range.Resolve();

        var csv = dto.ReportType.ToLowerInvariant() switch
        {
            "leads" => await ExportLeadsCsvAsync(start, end, ct),
            "emails" => await ExportEmailsCsvAsync(start, end, ct),
            "automation" => await ExportAutomationCsvAsync(start, end, ct),
            _ => await ExportDashboardCsvAsync(start, end, ct)
        };

        return Encoding.UTF8.GetBytes(csv);
    }

    public async Task<List<AnalyticsScheduledReport>> GetScheduledReportsAsync(Guid? orgId, CancellationToken ct = default)
    {
        return await _db.AnalyticsScheduledReports
            .Where(r => r.OrganizationId == orgId)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync(ct);
    }

    public async Task<AnalyticsScheduledReport> SaveScheduledReportAsync(SaveScheduledReportDto dto, Guid? orgId, CancellationToken ct = default)
    {
        var report = new AnalyticsScheduledReport
        {
            Name = dto.Name,
            ReportType = dto.ReportType,
            Frequency = dto.Frequency,
            Recipients = dto.Recipients,
            Format = dto.Format,
            FilterJson = dto.FilterJson,
            IsEnabled = dto.IsEnabled,
            OrganizationId = orgId,
            NextSendAt = ComputeNextSendAt(dto.Frequency)
        };

        _db.AnalyticsScheduledReports.Add(report);
        await _db.SaveChangesAsync(ct);
        return report;
    }

    public async Task DeleteScheduledReportAsync(Guid id, CancellationToken ct = default)
    {
        var report = await _db.AnalyticsScheduledReports.FindAsync([id], ct);
        if (report is null) return;

        _db.AnalyticsScheduledReports.Remove(report);
        await _db.SaveChangesAsync(ct);
    }

    // ── CSV Export helpers ────────────────────────────────────────────────────

    private async Task<string> ExportLeadsCsvAsync(DateTime start, DateTime end, CancellationToken ct)
    {
        var leads = await _db.Leads
            .Where(l => l.CreatedAt >= start && l.CreatedAt <= end)
            .OrderByDescending(l => l.CreatedAt)
            .Take(10000)
            .ToListAsync(ct);

        var sb = new StringBuilder();
        sb.AppendLine("Id,CompanyName,Status,Email,City,Industry,Score,CreatedAt");
        foreach (var l in leads)
            sb.AppendLine($"\"{l.Id}\",\"{Escape(l.CompanyName)}\",\"{l.Status}\",\"{Escape(l.Email)}\",\"{Escape(l.City)}\",\"{Escape(l.Industry)}\",{l.Score},\"{l.CreatedAt:yyyy-MM-dd HH:mm:ss}\"");

        return sb.ToString();
    }

    private async Task<string> ExportEmailsCsvAsync(DateTime start, DateTime end, CancellationToken ct)
    {
        var jobs = await _db.EmailSendJobs
            .Where(e => e.CreatedAt >= start && e.CreatedAt <= end)
            .OrderByDescending(e => e.CreatedAt)
            .Take(10000)
            .ToListAsync(ct);

        var sb = new StringBuilder();
        sb.AppendLine("Id,ToEmail,Subject,Status,SentAt,CreatedAt");
        foreach (var j in jobs)
            sb.AppendLine($"\"{j.Id}\",\"{Escape(j.ToEmail)}\",\"{Escape(j.Subject)}\",\"{j.Status}\",\"{j.SentAt:yyyy-MM-dd HH:mm:ss}\",\"{j.CreatedAt:yyyy-MM-dd HH:mm:ss}\"");

        return sb.ToString();
    }

    private async Task<string> ExportAutomationCsvAsync(DateTime start, DateTime end, CancellationToken ct)
    {
        var execs = await _db.AutomationExecutions
            .Where(e => e.CreatedAt >= start && e.CreatedAt <= end)
            .OrderByDescending(e => e.CreatedAt)
            .Take(10000)
            .ToListAsync(ct);

        var sb = new StringBuilder();
        sb.AppendLine("Id,WorkflowName,TriggerType,Status,DurationMs,RetryCount,StartedAt,CompletedAt");
        foreach (var e in execs)
            sb.AppendLine($"\"{e.Id}\",\"{Escape(e.WorkflowName)}\",\"{e.TriggerType}\",\"{e.Status}\",{e.DurationMs},{e.RetryCount},\"{e.StartedAt:yyyy-MM-dd HH:mm:ss}\",\"{e.CompletedAt:yyyy-MM-dd HH:mm:ss}\"");

        return sb.ToString();
    }

    private async Task<string> ExportDashboardCsvAsync(DateTime start, DateTime end, CancellationToken ct)
    {
        var totalLeads = await _db.Leads.CountAsync(l => l.CreatedAt >= start && l.CreatedAt <= end, ct);
        var converted = await _db.Leads.CountAsync(l => l.CreatedAt >= start && l.CreatedAt <= end && l.Status == LeadStatus.Client, ct);
        var emailsSent = await _db.EmailSendJobs.CountAsync(e => e.CreatedAt >= start && e.CreatedAt <= end && e.Status == EmailSendStatus.Sent, ct);

        var sb = new StringBuilder();
        sb.AppendLine("Metric,Value");
        sb.AppendLine($"\"Total Leads\",{totalLeads}");
        sb.AppendLine($"\"Converted\",{converted}");
        sb.AppendLine($"\"Conversion Rate\",{(totalLeads > 0 ? (double)converted / totalLeads * 100 : 0):F2}%");
        sb.AppendLine($"\"Emails Sent\",{emailsSent}");

        return sb.ToString();
    }

    private static string Escape(string? value)
    {
        if (value is null) return "";
        return value.Replace("\"", "\"\"");
    }

    internal static DateTime ComputeNextSendAt(ReportFrequency frequency)
    {
        var now = DateTime.UtcNow;
        return frequency switch
        {
            ReportFrequency.Daily => now.Date.AddDays(1).AddHours(8),
            ReportFrequency.Weekly => now.Date.AddDays(7 - (int)now.DayOfWeek + 1).AddHours(8),
            ReportFrequency.Monthly => new DateTime(now.Year, now.Month, 1, 8, 0, 0, DateTimeKind.Utc).AddMonths(1),
            _ => now.Date.AddDays(1).AddHours(8)
        };
    }
}
