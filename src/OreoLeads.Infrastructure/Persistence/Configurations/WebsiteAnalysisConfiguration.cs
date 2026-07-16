using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OreoLeads.Domain.Entities;

namespace OreoLeads.Infrastructure.Persistence.Configurations;

public class WebsiteAnalysisConfiguration : IEntityTypeConfiguration<WebsiteAnalysis>
{
    public void Configure(EntityTypeBuilder<WebsiteAnalysis> builder)
    {
        builder.ToTable("website_analyses");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Url).IsRequired().HasMaxLength(500);
        builder.Property(x => x.PageTitle).HasMaxLength(300);
        builder.Property(x => x.MetaDescription).HasMaxLength(500);
        builder.Property(x => x.CmsDetected).HasMaxLength(50);
        builder.Property(x => x.TechnologiesDetected).HasMaxLength(1000);
        builder.Property(x => x.Summary).HasMaxLength(3000);
        builder.Property(x => x.Recommendations).HasMaxLength(2000);
        builder.Property(x => x.AnalysisError).HasMaxLength(500);

        builder.HasIndex(x => x.LeadId);
        builder.HasIndex(x => x.CreatedAt);
    }
}
