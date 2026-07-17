using FluentAssertions;
using OreoLeads.Application.Features.Analytics.DTOs;
using OreoLeads.Domain.Enums;

namespace OreoLeads.Tests.Analytics;

public class AnalyticsReportTests
{
    [Fact]
    public async Task CreateReport_ValidDto_CreatesReport()
    {
        var (_, _, _, report, _) = AnalyticsTestHelpers.BuildServices();
        var dto = new CreateReportDto("Test Report", "A description", "dashboard", null, ReportFormat.Csv);

        var result = await report.CreateReportAsync(dto, null);

        result.Name.Should().Be("Test Report");
        result.Status.Should().Be(ReportStatus.Completed);
        result.Format.Should().Be(ReportFormat.Csv);
    }

    [Fact]
    public async Task GetReports_ReturnsForOrg()
    {
        var (_, _, _, report, _) = AnalyticsTestHelpers.BuildServices();
        await report.CreateReportAsync(new CreateReportDto("R1", null, "leads", null, ReportFormat.Csv), null);
        await report.CreateReportAsync(new CreateReportDto("R2", null, "emails", null, ReportFormat.Csv), null);

        var results = await report.GetReportsAsync(null);

        results.Should().HaveCount(2);
    }

    [Fact]
    public async Task ExportCsv_ReturnsBytes()
    {
        var (_, _, _, report, _) = AnalyticsTestHelpers.BuildServices();
        var dto = new ExportRequestDto("dashboard", DateRangePreset.Last30Days, null, null, ReportFormat.Csv);

        var bytes = await report.ExportAsync(dto, null);

        bytes.Should().NotBeEmpty();
        var csv = System.Text.Encoding.UTF8.GetString(bytes);
        csv.Should().Contain("Metric,Value");
    }

    [Fact]
    public async Task SaveScheduledReport_CreatesSchedule()
    {
        var (_, _, _, report, _) = AnalyticsTestHelpers.BuildServices();
        var dto = new SaveScheduledReportDto("Weekly Leads", "leads", ReportFrequency.Weekly, "admin@test.com", ReportFormat.Csv, null, true);

        var result = await report.SaveScheduledReportAsync(dto, null);

        result.Name.Should().Be("Weekly Leads");
        result.Frequency.Should().Be(ReportFrequency.Weekly);
        result.IsEnabled.Should().BeTrue();
        result.NextSendAt.Should().NotBeNull();
    }

    [Fact]
    public async Task DeleteScheduledReport_Removes()
    {
        var (_, _, _, report, _) = AnalyticsTestHelpers.BuildServices();
        var created = await report.SaveScheduledReportAsync(
            new SaveScheduledReportDto("ToDelete", "leads", ReportFrequency.Daily, "a@b.com", ReportFormat.Csv, null, true), null);

        await report.DeleteScheduledReportAsync(created.Id);

        var all = await report.GetScheduledReportsAsync(null);
        all.Should().NotContain(r => r.Id == created.Id);
    }
}
