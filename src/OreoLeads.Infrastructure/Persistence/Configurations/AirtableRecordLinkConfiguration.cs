using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OreoLeads.Domain.Entities;

namespace OreoLeads.Infrastructure.Persistence.Configurations;

public class AirtableRecordLinkConfiguration : IEntityTypeConfiguration<AirtableRecordLink>
{
    public void Configure(EntityTypeBuilder<AirtableRecordLink> builder)
    {
        builder.ToTable("airtable_record_links");
        builder.HasKey(x => x.Id);
        builder.HasIndex(x => new { x.LeadId, x.AirtableConfigurationId }).IsUnique();
        builder.HasIndex(x => x.AirtableRecordId);
        builder.Property(x => x.AirtableRecordId).HasMaxLength(50).IsRequired();
        builder.Property(x => x.LastSyncHash).HasMaxLength(100);
        builder.Property(x => x.ConflictStatus).HasConversion<string?>().HasMaxLength(50);
        builder.Property(x => x.ConflictOreoLeadsData).HasColumnType("text");
        builder.Property(x => x.ConflictAirtableData).HasColumnType("text");
        builder.Property(x => x.ConflictResolvedBy).HasMaxLength(200);
        builder.HasOne(x => x.Lead).WithMany().HasForeignKey(x => x.LeadId).OnDelete(DeleteBehavior.Cascade);
    }
}
