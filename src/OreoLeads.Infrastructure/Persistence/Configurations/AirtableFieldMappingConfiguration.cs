using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OreoLeads.Domain.Entities;

namespace OreoLeads.Infrastructure.Persistence.Configurations;

public class AirtableFieldMappingConfiguration : IEntityTypeConfiguration<AirtableFieldMapping>
{
    public void Configure(EntityTypeBuilder<AirtableFieldMapping> builder)
    {
        builder.ToTable("airtable_field_mappings");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.OreoLeadsField).HasMaxLength(100).IsRequired();
        builder.Property(x => x.AirtableFieldName).HasMaxLength(200).IsRequired();
        builder.Property(x => x.AirtableFieldType).HasConversion<string>().HasMaxLength(50);
        builder.Property(x => x.Direction).HasConversion<string>().HasMaxLength(50);
        builder.Property(x => x.DefaultValue).HasMaxLength(500);
        builder.Property(x => x.Transformation).HasMaxLength(1000);
    }
}
