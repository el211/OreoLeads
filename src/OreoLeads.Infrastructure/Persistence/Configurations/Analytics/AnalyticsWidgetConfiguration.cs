using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OreoLeads.Domain.Entities.Analytics;

namespace OreoLeads.Infrastructure.Persistence.Configurations.Analytics;

public class AnalyticsWidgetConfiguration : IEntityTypeConfiguration<AnalyticsWidget>
{
    public void Configure(EntityTypeBuilder<AnalyticsWidget> builder)
    {
        builder.ToTable("analytics_widgets");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Title).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Type).HasConversion<string>().HasMaxLength(50);
        builder.HasIndex(x => x.DashboardId);
        builder.HasOne(x => x.Dashboard).WithMany().HasForeignKey(x => x.DashboardId).OnDelete(DeleteBehavior.Cascade);
    }
}
