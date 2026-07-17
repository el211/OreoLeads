using FluentAssertions;
using OreoLeads.Application.Features.Analytics.DTOs;
using OreoLeads.Domain.Entities;
using OreoLeads.Domain.Enums;

namespace OreoLeads.Tests.Analytics;

public class AnalyticsFunnelTests
{
    [Fact]
    public async Task GetSalesFunnel_NoLeads_ReturnsEmptyStages()
    {
        var (svc, _, _, _, _) = AnalyticsTestHelpers.BuildServices();
        var range = new DateRangeDto(DateRangePreset.Last30Days, null, null);

        var result = await svc.GetSalesFunnelAsync(null, range);

        result.Stages.Should().NotBeNull();
        result.Stages.Should().AllSatisfy(s => s.Count.Should().Be(0));
    }

    [Fact]
    public async Task GetSalesFunnel_WithLeads_ReturnsStages()
    {
        var (svc, _, _, _, db) = AnalyticsTestHelpers.BuildServices();
        db.Leads.Add(new Lead { CompanyName = "A", Status = LeadStatus.New });
        db.Leads.Add(new Lead { CompanyName = "B", Status = LeadStatus.Qualified });
        db.Leads.Add(new Lead { CompanyName = "C", Status = LeadStatus.Client });
        await db.SaveChangesAsync();

        var range = new DateRangeDto(DateRangePreset.Last30Days, null, null);
        var result = await svc.GetSalesFunnelAsync(null, range);

        result.Stages.Should().NotBeEmpty();
        // New stage should show all leads at or above New
        var newStage = result.Stages.First(s => s.Name == "New");
        newStage.Count.Should().Be(3);
    }

    [Fact]
    public async Task FunnelStage_ConversionRate_IsCorrect()
    {
        var (svc, _, _, _, db) = AnalyticsTestHelpers.BuildServices();
        for (int i = 0; i < 10; i++)
            db.Leads.Add(new Lead { CompanyName = $"New{i}", Status = LeadStatus.New });
        for (int i = 0; i < 5; i++)
            db.Leads.Add(new Lead { CompanyName = $"Qual{i}", Status = LeadStatus.Qualified });
        await db.SaveChangesAsync();

        var range = new DateRangeDto(DateRangePreset.Last30Days, null, null);
        var result = await svc.GetSalesFunnelAsync(null, range);

        // "New" stage: all 15 leads are >= New
        var newStage = result.Stages.First(s => s.Name == "New");
        newStage.Count.Should().Be(15);

        // "Qualified" stage: 5 leads are >= Qualified
        var qualStage = result.Stages.First(s => s.Name == "Qualified");
        qualStage.Count.Should().Be(5);
    }

    [Fact]
    public async Task FunnelStage_DropsOff_WhenConversionIsLow()
    {
        var (svc, _, _, _, db) = AnalyticsTestHelpers.BuildServices();
        for (int i = 0; i < 20; i++)
            db.Leads.Add(new Lead { CompanyName = $"New{i}", Status = LeadStatus.New });
        db.Leads.Add(new Lead { CompanyName = "Client", Status = LeadStatus.Client });
        await db.SaveChangesAsync();

        var range = new DateRangeDto(DateRangePreset.Last30Days, null, null);
        var result = await svc.GetSalesFunnelAsync(null, range);

        var newStage = result.Stages.First(s => s.Name == "New");
        var clientStage = result.Stages.First(s => s.Name == "Client");

        // Significant dropoff from New to Client
        clientStage.Count.Should().BeLessThan(newStage.Count);
    }
}
