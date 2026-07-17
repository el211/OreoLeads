using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OreoLeads.Domain.Entities;

namespace OreoLeads.Infrastructure.Persistence.Configurations;

public class AirtableConfigurationConfiguration : IEntityTypeConfiguration<AirtableConfiguration>
{
    public void Configure(EntityTypeBuilder<AirtableConfiguration> builder)
    {
        builder.ToTable("airtable_configurations");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.ConnectionName).HasMaxLength(200).IsRequired();
        builder.Property(x => x.EncryptedAccessToken).HasMaxLength(1000);
        builder.Property(x => x.BaseId).HasMaxLength(100).IsRequired();
        builder.Property(x => x.TableIdOrName).HasMaxLength(200).IsRequired();
        builder.Property(x => x.SyncDirection).HasConversion<string>().HasMaxLength(50);
        builder.Property(x => x.ConflictStrategy).HasConversion<string>().HasMaxLength(50);
        builder.Property(x => x.WebhookId).HasMaxLength(200);
        builder.Property(x => x.WebhookCursor).HasMaxLength(500);
        builder.Property(x => x.IsEnabled).HasDefaultValue(false);
        builder.HasMany(x => x.FieldMappings)
               .WithOne(x => x.AirtableConfiguration)
               .HasForeignKey(x => x.AirtableConfigurationId)
               .OnDelete(DeleteBehavior.Cascade);
    }
}
