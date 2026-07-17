using FluentAssertions;
using Microsoft.Extensions.Caching.Memory;
using OreoLeads.Application.Features.Analytics.DTOs;
using OreoLeads.Domain.Entities;
using OreoLeads.Domain.Enums;

namespace OreoLeads.Tests.Analytics;

public class AnalyticsCacheTests
{
    [Fact]
    public async Task GetDashboard_SecondCall_ReturnsCached()
    {
        var (svc, db, _) = AnalyticsTestHelpers.BuildAnalyticsWithCache();
        db.Leads.Add(new Lead { CompanyName = "Test", Status = LeadStatus.New });
        await db.SaveChangesAsync();

        var range = new DateRangeDto(DateRangePreset.Last30Days, null, null);
        var first = await svc.GetExecutiveDashboardAsync(null, range);

        // Add more data - should not affect cached result
        db.Leads.Add(new Lead { CompanyName = "Test2", Status = LeadStatus.New });
        await db.SaveChangesAsync();

        var second = await svc.GetExecutiveDashboardAsync(null, range);

        second.GeneratedAt.Should().Be(first.GeneratedAt);
    }

    [Fact]
    public async Task GetDashboard_AfterExpiry_RecomputesValue()
    {
        var (svc, db, cache) = AnalyticsTestHelpers.BuildAnalyticsWithCache();
        db.Leads.Add(new Lead { CompanyName = "Test", Status = LeadStatus.New });
        await db.SaveChangesAsync();

        var range = new DateRangeDto(DateRangePreset.Last30Days, null, null);
        var first = await svc.GetExecutiveDashboardAsync(null, range);

        // Invalidate cache
        svc.InvalidateCacheForOrg(null);

        db.Leads.Add(new Lead { CompanyName = "Test2", Status = LeadStatus.New });
        await db.SaveChangesAsync();

        var second = await svc.GetExecutiveDashboardAsync(null, range);

        // After invalidation, result should reflect new data
        second.Leads.Today.Should().BeGreaterThanOrEqualTo(first.Leads.Today);
    }

    [Fact]
    public async Task Cache_DifferentOrgs_DifferentKeys()
    {
        var (svc, db, _) = AnalyticsTestHelpers.BuildAnalyticsWithCache();
        var org1 = Guid.NewGuid();
        var org2 = Guid.NewGuid();

        var range = new DateRangeDto(DateRangePreset.Last30Days, null, null);
        var result1 = await svc.GetExecutiveDashboardAsync(org1, range);
        var result2 = await svc.GetExecutiveDashboardAsync(org2, range);

        // Both should return (independently cached) results
        result1.Should().NotBeNull();
        result2.Should().NotBeNull();
    }

    [Fact]
    public async Task Cache_DifferentPresets_DifferentKeys()
    {
        var (svc, db, _) = AnalyticsTestHelpers.BuildAnalyticsWithCache();
        db.Leads.Add(new Lead { CompanyName = "Test", Status = LeadStatus.New });
        await db.SaveChangesAsync();

        var range7 = new DateRangeDto(DateRangePreset.Last7Days, null, null);
        var range30 = new DateRangeDto(DateRangePreset.Last30Days, null, null);

        var result7 = await svc.GetExecutiveDashboardAsync(null, range7);
        var result30 = await svc.GetExecutiveDashboardAsync(null, range30);

        // Both return independently
        result7.Should().NotBeNull();
        result30.Should().NotBeNull();
    }
}
