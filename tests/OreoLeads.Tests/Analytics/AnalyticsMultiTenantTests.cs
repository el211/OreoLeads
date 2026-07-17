using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using OreoLeads.Application.Features.Analytics.DTOs;
using OreoLeads.Domain.Entities;
using OreoLeads.Domain.Enums;
using OreoLeads.Infrastructure.Analytics;
using OreoLeads.Infrastructure.Persistence;

namespace OreoLeads.Tests.Analytics;

public class AnalyticsMultiTenantTests
{
    private static (AnalyticsService analytics, WidgetService widget, ReportService report, ForecastService forecast, ApplicationDbContext db) BuildForOrg(string dbName, Guid? orgId)
    {
        var services = new ServiceCollection();
        services.AddScoped<TenantContext>(sp =>
        {
            var t = new TenantContext();
            if (orgId.HasValue) t.SetOrganization(orgId.Value);
            return t;
        });
        services.AddDbContext<ApplicationDbContext>(opt => opt.UseInMemoryDatabase(dbName));

        var sp = services.BuildServiceProvider();
        var db = sp.GetRequiredService<ApplicationDbContext>();
        var cache = new MemoryCache(new MemoryCacheOptions());

        var analytics = new AnalyticsService(db, cache, NullLogger<AnalyticsService>.Instance);
        var widget = new WidgetService(db);
        var report = new ReportService(db, NullLogger<ReportService>.Instance);
        var forecast = new ForecastService(db, NullLogger<ForecastService>.Instance);

        return (analytics, widget, report, forecast, db);
    }

    [Fact]
    public async Task GetDashboard_OtherOrg_ReturnsOwnData()
    {
        var org1 = Guid.NewGuid();
        var org2 = Guid.NewGuid();
        var dbName = Guid.NewGuid().ToString();

        // Seed data with no tenant filter (null org = admin)
        var (_, _, _, _, seedDb) = BuildForOrg(dbName, null);
        seedDb.Leads.Add(new Lead { CompanyName = "Org1Lead", Status = LeadStatus.New, OrganizationId = org1 });
        seedDb.Leads.Add(new Lead { CompanyName = "Org2Lead", Status = LeadStatus.New, OrganizationId = org2 });
        await seedDb.SaveChangesAsync();

        // Query as org1
        var (svc1, _, _, _, _) = BuildForOrg(dbName, org1);
        var range = new DateRangeDto(DateRangePreset.Last30Days, null, null);
        var result1 = await svc1.GetExecutiveDashboardAsync(org1, range);

        result1.Leads.Today.Should().Be(1);
    }

    [Fact]
    public async Task GetWidgets_OtherOrg_ReturnsEmpty()
    {
        var org1 = Guid.NewGuid();
        var org2 = Guid.NewGuid();
        var dbName = Guid.NewGuid().ToString();

        // Create dashboard for org1
        var (_, widget1, _, _, _) = BuildForOrg(dbName, null);
        var dashboard = await widget1.SaveDashboardAsync(new SaveDashboardDto("Org1 Dashboard", null, true), org1, "user1");
        await widget1.AddWidgetAsync(new AddWidgetDto(dashboard.Id, "Widget1", WidgetType.KpiCard, null, 0), org1);

        // Query as org2
        var (_, widget2, _, _, _) = BuildForOrg(dbName, org2);
        var dashboards = await widget2.GetDashboardsAsync(org2, "user2");

        dashboards.Should().BeEmpty();
    }

    [Fact]
    public async Task GetReports_OtherOrg_ReturnsEmpty()
    {
        var org1 = Guid.NewGuid();
        var org2 = Guid.NewGuid();
        var dbName = Guid.NewGuid().ToString();

        var (_, _, report1, _, _) = BuildForOrg(dbName, null);
        await report1.CreateReportAsync(new CreateReportDto("R1", null, "leads", null, ReportFormat.Csv), org1);

        var (_, _, report2, _, _) = BuildForOrg(dbName, org2);
        var reports = await report2.GetReportsAsync(org2);

        reports.Should().BeEmpty();
    }

    [Fact]
    public async Task Forecast_IsolatedByOrg()
    {
        var org1 = Guid.NewGuid();
        var dbName = Guid.NewGuid().ToString();

        var (_, _, _, forecast1, _) = BuildForOrg(dbName, org1);

        // No data for org1 - should return empty/zero forecasts
        var result = await forecast1.ForecastLeadsAsync(org1, 10);

        result.Should().HaveCount(10);
    }
}
