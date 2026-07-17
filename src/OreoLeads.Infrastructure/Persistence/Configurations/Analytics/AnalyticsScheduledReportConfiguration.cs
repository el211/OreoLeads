using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OreoLeads.Domain.Entities.Analytics;

namespace OreoLeads.Infrastructure.Persistence.Configurations.Analytics;

public class AnalyticsScheduledReportConfiguration : IEntityTypeConfiguration<AnalyticsScheduledReport>
{
    public void Configure(EntityTypeBuilder<AnalyticsScheduledReport> builder)
    {
        builder.ToTable("analytics_scheduled_reports");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Name).HasMaxLength(200).IsRequired();
        builder.Property(x => x.ReportType).HasMaxLength(100).IsRequired();
        builder.Property(x => x.Frequency).HasConversion<string>().HasMaxLength(50);
        builder.Property(x => x.Recipients).HasMaxLength(2000).IsRequired();
        builder.Property(x => x.Format).HasConversion<string>().HasMaxLength(50);
        builder.HasIndex(x => x.OrganizationId);
        builder.HasIndex(x => x.NextSendAt);
    }
}
