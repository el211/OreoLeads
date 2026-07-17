using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OreoLeads.Domain.Entities.Analytics;

namespace OreoLeads.Infrastructure.Persistence.Configurations.Analytics;

public class AnalyticsDashboardConfiguration : IEntityTypeConfiguration<AnalyticsDashboard>
{
    public void Configure(EntityTypeBuilder<AnalyticsDashboard> builder)
    {
        builder.ToTable("analytics_dashboards");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Name).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Description).HasMaxLength(1000);
        builder.Property(x => x.UserId).HasMaxLength(450);
        builder.HasIndex(x => x.OrganizationId);
        builder.HasIndex(x => x.UserId);
    }
}
