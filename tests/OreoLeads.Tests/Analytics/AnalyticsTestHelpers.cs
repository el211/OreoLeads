using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using OreoLeads.Infrastructure.Analytics;
using OreoLeads.Infrastructure.Persistence;

namespace OreoLeads.Tests.Analytics;

internal static class AnalyticsTestHelpers
{
    internal static (AnalyticsService analytics, ForecastService forecast, WidgetService widget, ReportService report, ApplicationDbContext db) BuildServices()
    {
        var dbName = Guid.NewGuid().ToString();
        var services = new ServiceCollection();
        services.AddScoped<TenantContext>();
        services.AddDbContext<ApplicationDbContext>(opt => opt.UseInMemoryDatabase(dbName));

        var sp = services.BuildServiceProvider();
        var db = sp.GetRequiredService<ApplicationDbContext>();
        var cache = new MemoryCache(new MemoryCacheOptions());

        var analytics = new AnalyticsService(db, cache, NullLogger<AnalyticsService>.Instance);
        var forecast = new ForecastService(db, NullLogger<ForecastService>.Instance);
        var widget = new WidgetService(db);
        var report = new ReportService(db, NullLogger<ReportService>.Instance);

        return (analytics, forecast, widget, report, db);
    }

    internal static (AnalyticsService analytics, ApplicationDbContext db, MemoryCache cache) BuildAnalyticsWithCache()
    {
        var dbName = Guid.NewGuid().ToString();
        var services = new ServiceCollection();
        services.AddScoped<TenantContext>();
        services.AddDbContext<ApplicationDbContext>(opt => opt.UseInMemoryDatabase(dbName));

        var sp = services.BuildServiceProvider();
        var db = sp.GetRequiredService<ApplicationDbContext>();
        var cache = new MemoryCache(new MemoryCacheOptions());

        var analytics = new AnalyticsService(db, cache, NullLogger<AnalyticsService>.Instance);

        return (analytics, db, cache);
    }
}
