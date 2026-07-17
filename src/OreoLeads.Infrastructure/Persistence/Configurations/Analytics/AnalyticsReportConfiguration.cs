using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OreoLeads.Domain.Entities.Analytics;

namespace OreoLeads.Infrastructure.Persistence.Configurations.Analytics;

public class AnalyticsReportConfiguration : IEntityTypeConfiguration<AnalyticsReport>
{
    public void Configure(EntityTypeBuilder<AnalyticsReport> builder)
    {
        builder.ToTable("analytics_reports");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Name).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Description).HasMaxLength(1000);
        builder.Property(x => x.ReportType).HasMaxLength(100).IsRequired();
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(50);
        builder.Property(x => x.Format).HasConversion<string>().HasMaxLength(50);
        builder.Property(x => x.FilePath).HasMaxLength(500);
        builder.Property(x => x.ErrorMessage).HasMaxLength(2000);
        builder.HasIndex(x => x.OrganizationId);
        builder.HasIndex(x => x.Status);
    }
}
