using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OreoLeads.Domain.Entities.Analytics;

namespace OreoLeads.Infrastructure.Persistence.Configurations.Analytics;

public class AnalyticsForecastConfiguration : IEntityTypeConfiguration<AnalyticsForecast>
{
    public void Configure(EntityTypeBuilder<AnalyticsForecast> builder)
    {
        builder.ToTable("analytics_forecasts");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.MetricName).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Period).HasMaxLength(100).IsRequired();
        builder.Property(x => x.Method).HasConversion<string>().HasMaxLength(50);
        builder.HasIndex(x => x.OrganizationId);
        builder.HasIndex(x => x.MetricName);
    }
}
