using FluentAssertions;
using OreoLeads.Infrastructure.Analytics;

namespace OreoLeads.Tests.Analytics;

public class AnalyticsForecastTests
{
    [Fact]
    public async Task ForecastLeads_NoData_ReturnsEmpty()
    {
        var (_, forecast, _, _, _) = AnalyticsTestHelpers.BuildServices();

        var result = await forecast.ForecastLeadsAsync(null, 10);

        // With no data, returns flat forecast at 0
        result.Should().HaveCount(10);
        result.Should().AllSatisfy(p => p.Value.Should().Be(0));
    }

    [Fact]
    public void ForecastLeads_WithData_ReturnsProjection()
    {
        var data = new List<(int Day, double Count)>
        {
            (0, 5), (1, 6), (2, 7), (3, 8), (4, 9), (5, 10)
        };

        var result = ForecastService.GenerateForecast(data, 5);

        result.Should().HaveCount(5);
        result.Should().AllSatisfy(p => p.Value.Should().BeGreaterThan(0));
    }

    [Fact]
    public void ForecastLeads_PositiveTrend_ProjectsIncrease()
    {
        var data = new List<(int Day, double Count)>
        {
            (0, 2), (1, 4), (2, 6), (3, 8), (4, 10)
        };

        var result = ForecastService.GenerateForecast(data, 3);

        // Each subsequent point should be higher (positive trend)
        for (int i = 1; i < result.Count; i++)
            result[i].Value.Should().BeGreaterThan(result[i - 1].Value);
    }

    [Fact]
    public void LinearRegression_CalculatesCorrectSlope()
    {
        // y = 2 + 3x: points (0,2), (1,5), (2,8)
        var data = new List<(int Day, double Count)>
        {
            (0, 2), (1, 5), (2, 8)
        };

        var (a, b) = ForecastService.LinearRegression(data);

        a.Should().BeApproximately(2, 0.01);
        b.Should().BeApproximately(3, 0.01);
    }

    [Fact]
    public async Task ForecastSummary_ReturnsBothForecasts()
    {
        var (_, forecast, _, _, _) = AnalyticsTestHelpers.BuildServices();

        var result = await forecast.GetForecastSummaryAsync(null);

        result.LeadsForecast.Should().NotBeNull();
        result.ConversionsForecast.Should().NotBeNull();
        result.EmailsForecast.Should().NotBeNull();
    }

    [Fact]
    public void ConfidenceInterval_IsAroundPrediction()
    {
        var data = new List<(int Day, double Count)>
        {
            (0, 10), (1, 12), (2, 14), (3, 16), (4, 18)
        };

        var result = ForecastService.GenerateForecast(data, 3);

        result.Should().AllSatisfy(p =>
        {
            p.ConfidenceLow.Should().BeLessThanOrEqualTo(p.Value);
            p.ConfidenceHigh.Should().BeGreaterThanOrEqualTo(p.Value);
        });
    }
}
