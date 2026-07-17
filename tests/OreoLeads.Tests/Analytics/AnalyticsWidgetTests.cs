using FluentAssertions;
using OreoLeads.Application.Features.Analytics.DTOs;
using OreoLeads.Domain.Enums;

namespace OreoLeads.Tests.Analytics;

public class AnalyticsWidgetTests
{
    [Fact]
    public async Task GetOrCreateDefaultDashboard_NoDashboard_CreatesDefault()
    {
        var (_, _, widget, _, _) = AnalyticsTestHelpers.BuildServices();

        var dashboard = await widget.GetOrCreateDefaultDashboardAsync(null, "user1");

        dashboard.Should().NotBeNull();
        dashboard.Name.Should().Be("Dashboard principal");
        dashboard.IsDefault.Should().BeTrue();
    }

    [Fact]
    public async Task GetOrCreateDefaultDashboard_ExistingDashboard_ReturnsExisting()
    {
        var (_, _, widget, _, _) = AnalyticsTestHelpers.BuildServices();

        var first = await widget.GetOrCreateDefaultDashboardAsync(null, "user1");
        var second = await widget.GetOrCreateDefaultDashboardAsync(null, "user1");

        second.Id.Should().Be(first.Id);
    }

    [Fact]
    public async Task AddWidget_ValidWidget_CreatesWidget()
    {
        var (_, _, widget, _, _) = AnalyticsTestHelpers.BuildServices();
        var dashboard = await widget.GetOrCreateDefaultDashboardAsync(null, "user1");

        var dto = new AddWidgetDto(dashboard.Id, "Custom Widget", WidgetType.BarChart, null, 10);
        var result = await widget.AddWidgetAsync(dto, null);

        result.Title.Should().Be("Custom Widget");
        result.Type.Should().Be(WidgetType.BarChart);
    }

    [Fact]
    public async Task DeleteWidget_Existing_Removes()
    {
        var (_, _, widget, _, _) = AnalyticsTestHelpers.BuildServices();
        var dashboard = await widget.GetOrCreateDefaultDashboardAsync(null, "user1");
        var dto = new AddWidgetDto(dashboard.Id, "ToDelete", WidgetType.KpiCard, null, 0);
        var created = await widget.AddWidgetAsync(dto, null);

        await widget.DeleteWidgetAsync(created.Id);

        var widgets = await widget.GetWidgetsAsync(dashboard.Id);
        widgets.Should().NotContain(w => w.Id == created.Id);
    }

    [Fact]
    public async Task UpdateWidget_ChangesTitle()
    {
        var (_, _, widget, _, _) = AnalyticsTestHelpers.BuildServices();
        var dashboard = await widget.GetOrCreateDefaultDashboardAsync(null, "user1");
        var dto = new AddWidgetDto(dashboard.Id, "Original", WidgetType.KpiCard, null, 0);
        var created = await widget.AddWidgetAsync(dto, null);

        var updated = await widget.UpdateWidgetAsync(created.Id, new UpdateWidgetDto("Updated", null, null, true));

        updated.Title.Should().Be("Updated");
    }

    [Fact]
    public async Task SaveLayout_UpdatesJson()
    {
        var (_, _, widget, _, db) = AnalyticsTestHelpers.BuildServices();
        var dashboard = await widget.GetOrCreateDefaultDashboardAsync(null, "user1");

        await widget.SaveLayoutAsync(dashboard.Id, "{\"columns\":3}");

        var updated = await db.AnalyticsDashboards.FindAsync(dashboard.Id);
        updated!.LayoutJson.Should().Be("{\"columns\":3}");
    }
}
