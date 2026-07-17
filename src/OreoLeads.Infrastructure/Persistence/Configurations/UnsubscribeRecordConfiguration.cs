using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OreoLeads.Domain.Entities;

namespace OreoLeads.Infrastructure.Persistence.Configurations;

public class UnsubscribeRecordConfiguration : IEntityTypeConfiguration<UnsubscribeRecord>
{
    public void Configure(EntityTypeBuilder<UnsubscribeRecord> builder)
    {
        builder.ToTable("unsubscribe_records");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Email).HasMaxLength(320).IsRequired();
        builder.Property(x => x.Source).HasMaxLength(50).HasDefaultValue("webhook");
        builder.Property(x => x.Reason).HasMaxLength(500);

        builder.HasIndex(x => x.Email)
               .IsUnique()
               .HasDatabaseName("ix_unsubscribe_records_email_unique");

        builder.HasIndex(x => x.LeadId)
               .HasDatabaseName("ix_unsubscribe_records_lead_id");
    }
}
