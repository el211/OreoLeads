using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OreoLeads.Application.Common.Interfaces;
using OreoLeads.Application.Features.Analytics.DTOs;
using OreoLeads.Domain.Enums;
using OreoLeads.Infrastructure.Persistence;

namespace OreoLeads.Infrastructure.Analytics;

internal sealed class ForecastService : IForecastService
{
    private readonly ApplicationDbContext _db;
    private readonly ILogger<ForecastService> _logger;

    public ForecastService(ApplicationDbContext db, ILogger<ForecastService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<List<ForecastPointDto>> ForecastLeadsAsync(Guid? orgId, int daysAhead, CancellationToken ct = default)
    {
        var historicalData = await GetDailyCountsAsync(
            _db.Leads.Where(l => l.CreatedAt >= DateTime.UtcNow.AddDays(-30)),
            l => l.CreatedAt.Date, ct);

        return GenerateForecast(historicalData, daysAhead);
    }

    public async Task<List<ForecastPointDto>> ForecastConversionsAsync(Guid? orgId, int daysAhead, CancellationToken ct = default)
    {
        var historicalData = await GetDailyCountsAsync(
            _db.Leads.Where(l => l.CreatedAt >= DateTime.UtcNow.AddDays(-30) && l.Status == LeadStatus.Client),
            l => l.CreatedAt.Date, ct);

        return GenerateForecast(historicalData, daysAhead);
    }

    public async Task<List<ForecastPointDto>> ForecastEmailsAsync(Guid? orgId, int daysAhead, CancellationToken ct = default)
    {
        var historicalData = await GetDailyCountsAsync(
            _db.EmailSendJobs.Where(e => e.CreatedAt >= DateTime.UtcNow.AddDays(-30) && e.Status == EmailSendStatus.Sent),
            e => e.CreatedAt.Date, ct);

        return GenerateForecast(historicalData, daysAhead);
    }

    public async Task<ForecastSummaryDto> GetForecastSummaryAsync(Guid? orgId, CancellationToken ct = default)
    {
        var leads = await ForecastLeadsAsync(orgId, 30, ct);
        var conversions = await ForecastConversionsAsync(orgId, 30, ct);
        var emails = await ForecastEmailsAsync(orgId, 30, ct);

        var projectedLeads = leads.Count > 0 ? leads.Sum(p => p.Value) : 0;
        var projectedConversions = conversions.Count > 0 ? conversions.Sum(p => p.Value) : 0;

        return new ForecastSummaryDto(leads, conversions, emails,
            Math.Round(projectedLeads, 1), Math.Round(projectedConversions, 1));
    }

    // ── Internal helpers ─────────────────────────────────────────────────────

    private static async Task<List<(int Day, double Count)>> GetDailyCountsAsync<T>(
        IQueryable<T> query,
        System.Linq.Expressions.Expression<Func<T, DateTime>> dateSelector,
        CancellationToken ct)
    {
        var grouped = await query
            .GroupBy(dateSelector)
            .Select(g => new { Date = g.Key, Count = g.Count() })
            .OrderBy(x => x.Date)
            .ToListAsync(ct);

        if (grouped.Count == 0)
            return new List<(int, double)>();

        var baseDate = grouped[0].Date;
        return grouped.Select(g => ((int)(g.Date - baseDate).TotalDays, (double)g.Count)).ToList();
    }

    internal static List<ForecastPointDto> GenerateForecast(List<(int Day, double Count)> data, int daysAhead)
    {
        if (data.Count < 3)
        {
            // Not enough data: return flat forecast at average
            var avg = data.Count > 0 ? data.Average(d => d.Count) : 0;
            var result = new List<ForecastPointDto>();
            for (int i = 1; i <= daysAhead; i++)
            {
                var date = DateTime.UtcNow.Date.AddDays(i);
                result.Add(new ForecastPointDto(date, Math.Max(0, avg), 0, Math.Max(0, avg * 1.2)));
            }
            return result;
        }

        // Linear regression: y = a + b*x
        var (a, b) = LinearRegression(data);

        var lastDay = data.Max(d => d.Day);
        var forecast = new List<ForecastPointDto>();

        for (int i = 1; i <= daysAhead; i++)
        {
            var x = lastDay + i;
            var value = Math.Max(0, a + b * x);
            var confidence = value * 0.2; // +/- 20%
            var date = DateTime.UtcNow.Date.AddDays(i);
            forecast.Add(new ForecastPointDto(date, Math.Round(value, 2),
                Math.Round(Math.Max(0, value - confidence), 2),
                Math.Round(value + confidence, 2)));
        }

        return forecast;
    }

    internal static (double a, double b) LinearRegression(List<(int Day, double Count)> data)
    {
        var n = data.Count;
        double sumX = 0, sumY = 0, sumXY = 0, sumX2 = 0;

        foreach (var (x, y) in data)
        {
            sumX += x;
            sumY += y;
            sumXY += x * y;
            sumX2 += x * x;
        }

        var denominator = n * sumX2 - sumX * sumX;
        if (Math.Abs(denominator) < 1e-10)
            return (sumY / n, 0);

        var bSlope = (n * sumXY - sumX * sumY) / denominator;
        var aIntercept = (sumY - bSlope * sumX) / n;

        return (aIntercept, bSlope);
    }
}
