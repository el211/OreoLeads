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
        builder.Property(x => x.TechStack).HasMaxLength(2000);
        builder.Property(x => x.MetaTitle).HasMaxLength(200);
        builder.Property(x => x.MetaDescription).HasMaxLength(500);
        builder.Property(x => x.Notes).HasMaxLength(2000);
        builder.HasIndex(x => x.LeadId);
    }
}
