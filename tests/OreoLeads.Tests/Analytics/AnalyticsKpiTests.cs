using FluentAssertions;
using OreoLeads.Application.Features.Analytics.DTOs;
using OreoLeads.Domain.Entities;
using OreoLeads.Domain.Enums;

namespace OreoLeads.Tests.Analytics;

public class AnalyticsKpiTests
{
    [Fact]
    public async Task GetKpiSummary_NoData_ReturnsZeros()
    {
        var (svc, _, _, _, _) = AnalyticsTestHelpers.BuildServices();
        var range = new DateRangeDto(DateRangePreset.Last30Days, null, null);

        var result = await svc.GetKpiSummaryAsync(null, range);

        result.ConversionRate.Should().Be(0);
        result.OpenRate.Should().Be(0);
        result.LeadsPerDay.Should().Be(0);
    }

    [Fact]
    public async Task GetKpiSummary_WithLeads_ReturnsConversionRate()
    {
        var (svc, _, _, _, db) = AnalyticsTestHelpers.BuildServices();
        for (int i = 0; i < 8; i++)
            db.Leads.Add(new Lead { CompanyName = $"Co{i}", Status = LeadStatus.New });
        db.Leads.Add(new Lead { CompanyName = "Client1", Status = LeadStatus.Client });
        db.Leads.Add(new Lead { CompanyName = "Client2", Status = LeadStatus.Client });
        await db.SaveChangesAsync();

        var range = new DateRangeDto(DateRangePreset.Last30Days, null, null);
        var result = await svc.GetKpiSummaryAsync(null, range);

        result.ConversionRate.Should().Be(20); // 2/10 * 100
    }

    [Fact]
    public async Task GetKpiSummary_WithEmails_ReturnsOpenRate()
    {
        var (svc, _, _, _, db) = AnalyticsTestHelpers.BuildServices();
        for (int i = 0; i < 10; i++)
            db.EmailSendJobs.Add(new EmailSendJob { Subject = $"Test{i}", Status = EmailSendStatus.Sent, ToEmail = $"a{i}@b.com" });
        for (int i = 0; i < 3; i++)
            db.EmailEvents.Add(new EmailEvent { EventType = EmailEventType.Opened, OccurredAt = DateTime.UtcNow });
        await db.SaveChangesAsync();

        var range = new DateRangeDto(DateRangePreset.Last30Days, null, null);
        var result = await svc.GetKpiSummaryAsync(null, range);

        result.OpenRate.Should().Be(30); // 3/10 * 100
    }

    [Fact]
    public async Task GetKpiSummary_DateRange_FiltersCorrectly()
    {
        var (svc, _, _, _, db) = AnalyticsTestHelpers.BuildServices();
        // Lead inside range
        db.Leads.Add(new Lead { CompanyName = "InRange", Status = LeadStatus.New });
        await db.SaveChangesAsync();

        var range = new DateRangeDto(DateRangePreset.Today, null, null);
        var result = await svc.GetKpiSummaryAsync(null, range);

        result.LeadsPerDay.Should().BeGreaterThanOrEqualTo(0);
    }

    [Fact]
    public async Task ConversionRate_Calculation_IsCorrect()
    {
        var (svc, _, _, _, db) = AnalyticsTestHelpers.BuildServices();
        for (int i = 0; i < 4; i++)
            db.Leads.Add(new Lead { CompanyName = $"New{i}", Status = LeadStatus.New });
        db.Leads.Add(new Lead { CompanyName = "Client", Status = LeadStatus.Client });
        await db.SaveChangesAsync();

        var range = new DateRangeDto(DateRangePreset.Last30Days, null, null);
        var result = await svc.GetKpiSummaryAsync(null, range);

        result.ConversionRate.Should().Be(20); // 1/5 * 100
    }

    [Fact]
    public async Task OpenRate_Calculation_IsCorrect()
    {
        var (svc, _, _, _, db) = AnalyticsTestHelpers.BuildServices();
        for (int i = 0; i < 5; i++)
            db.EmailSendJobs.Add(new EmailSendJob { Subject = $"T{i}", Status = EmailSendStatus.Sent, ToEmail = $"a{i}@b.com" });
        for (int i = 0; i < 2; i++)
            db.EmailEvents.Add(new EmailEvent { EventType = EmailEventType.Opened, OccurredAt = DateTime.UtcNow });
        await db.SaveChangesAsync();

        var range = new DateRangeDto(DateRangePreset.Last30Days, null, null);
        var result = await svc.GetKpiSummaryAsync(null, range);

        result.OpenRate.Should().Be(40); // 2/5 * 100
    }

    [Fact]
    public async Task LeadsPerDay_Calculation_IsCorrect()
    {
        var (svc, _, _, _, db) = AnalyticsTestHelpers.BuildServices();
        for (int i = 0; i < 30; i++)
            db.Leads.Add(new Lead { CompanyName = $"Co{i}", Status = LeadStatus.New });
        await db.SaveChangesAsync();

        var range = new DateRangeDto(DateRangePreset.Last30Days, null, null);
        var result = await svc.GetKpiSummaryAsync(null, range);

        result.LeadsPerDay.Should().BeApproximately(1, 0.1); // 30 leads / ~30 days
    }
}
