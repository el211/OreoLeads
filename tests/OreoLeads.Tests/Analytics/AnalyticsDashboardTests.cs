using FluentAssertions;
using OreoLeads.Application.Features.Analytics.DTOs;
using OreoLeads.Domain.Entities;
using OreoLeads.Domain.Enums;

namespace OreoLeads.Tests.Analytics;

public class AnalyticsDashboardTests
{
    [Fact]
    public async Task GetExecutiveDashboard_NoData_ReturnsZeroStats()
    {
        var (svc, _, _, _, _) = AnalyticsTestHelpers.BuildServices();
        var range = new DateRangeDto(DateRangePreset.Last30Days, null, null);

        var result = await svc.GetExecutiveDashboardAsync(null, range);

        result.Leads.Today.Should().Be(0);
        result.Emails.Sent.Should().Be(0);
        result.Automation.TotalExecutions.Should().Be(0);
        result.Airtable.TotalSyncs.Should().Be(0);
    }

    [Fact]
    public async Task GetExecutiveDashboard_WithLeads_ReturnsLeadCount()
    {
        var (svc, _, _, _, db) = AnalyticsTestHelpers.BuildServices();
        db.Leads.Add(new Lead { CompanyName = "TestCo1", Status = LeadStatus.New });
        db.Leads.Add(new Lead { CompanyName = "TestCo2", Status = LeadStatus.Qualified });
        db.Leads.Add(new Lead { CompanyName = "TestCo3", Status = LeadStatus.Client });
        await db.SaveChangesAsync();

        var range = new DateRangeDto(DateRangePreset.Last30Days, null, null);
        var result = await svc.GetExecutiveDashboardAsync(null, range);

        result.Leads.Today.Should().Be(3);
        result.Leads.NewProspects.Should().Be(1);
        result.Leads.Converted.Should().Be(1);
    }

    [Fact]
    public async Task GetExecutiveDashboard_WithEmails_ReturnsEmailStats()
    {
        var (svc, _, _, _, db) = AnalyticsTestHelpers.BuildServices();
        db.EmailSendJobs.Add(new EmailSendJob { Subject = "Test", Status = EmailSendStatus.Sent, ToEmail = "a@b.com" });
        db.EmailEvents.Add(new EmailEvent { EventType = EmailEventType.Opened, OccurredAt = DateTime.UtcNow });
        db.EmailEvents.Add(new EmailEvent { EventType = EmailEventType.Clicked, OccurredAt = DateTime.UtcNow });
        await db.SaveChangesAsync();

        var range = new DateRangeDto(DateRangePreset.Last30Days, null, null);
        var result = await svc.GetExecutiveDashboardAsync(null, range);

        result.Emails.Sent.Should().Be(1);
        result.Emails.Opened.Should().Be(1);
        result.Emails.Clicked.Should().Be(1);
    }

    [Fact]
    public async Task GetExecutiveDashboard_DateRange_Filters()
    {
        var (svc, _, _, _, db) = AnalyticsTestHelpers.BuildServices();
        db.Leads.Add(new Lead { CompanyName = "Today", Status = LeadStatus.New });
        await db.SaveChangesAsync();

        var range = new DateRangeDto(DateRangePreset.Today, null, null);
        var result = await svc.GetExecutiveDashboardAsync(null, range);

        result.Leads.Today.Should().BeGreaterThanOrEqualTo(0);
        result.GeneratedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void DateRange_Today_ReturnsCorrectDates()
    {
        var range = new DateRangeDto(DateRangePreset.Today, null, null);
        var (start, end) = range.Resolve();

        start.Should().Be(DateTime.UtcNow.Date);
        end.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(2));
    }

    [Fact]
    public void DateRange_Last30Days_ReturnsCorrectDates()
    {
        var range = new DateRangeDto(DateRangePreset.Last30Days, null, null);
        var (start, end) = range.Resolve();

        start.Should().Be(DateTime.UtcNow.Date.AddDays(-30));
        end.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(2));
    }
}
