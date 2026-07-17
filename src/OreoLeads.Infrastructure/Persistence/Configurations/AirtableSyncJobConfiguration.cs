using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OreoLeads.Domain.Entities;

namespace OreoLeads.Infrastructure.Persistence.Configurations;

public class AirtableSyncJobConfiguration : IEntityTypeConfiguration<AirtableSyncJob>
{
    public void Configure(EntityTypeBuilder<AirtableSyncJob> builder)
    {
        builder.ToTable("airtable_sync_jobs");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(50);
        builder.Property(x => x.Direction).HasConversion<string>().HasMaxLength(50);
        builder.Property(x => x.TriggerReason).HasMaxLength(100);
        builder.Property(x => x.LeadFilter).HasMaxLength(2000);
        builder.Property(x => x.ErrorMessage).HasMaxLength(2000);
        builder.Property(x => x.AirtableOffset).HasMaxLength(500);
        builder.HasMany(x => x.Logs)
               .WithOne(x => x.AirtableSyncJob)
               .HasForeignKey(x => x.AirtableSyncJobId)
               .OnDelete(DeleteBehavior.Cascade);
    }
}
