using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OreoLeads.Domain.Entities;

namespace OreoLeads.Infrastructure.Persistence.Configurations;

public class AirtableSyncLogConfiguration : IEntityTypeConfiguration<AirtableSyncLog>
{
    public void Configure(EntityTypeBuilder<AirtableSyncLog> builder)
    {
        builder.ToTable("airtable_sync_logs");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Action).HasMaxLength(50).IsRequired();
        builder.Property(x => x.AirtableRecordId).HasMaxLength(50);
        builder.Property(x => x.Details).HasMaxLength(5000);
        builder.Property(x => x.ErrorMessage).HasMaxLength(2000);
    }
}
