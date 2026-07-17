using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OreoLeads.Domain.Entities;

namespace OreoLeads.Infrastructure.Persistence.Configurations;

public class BrevoConfigurationConfiguration : IEntityTypeConfiguration<BrevoConfiguration>
{
    public void Configure(EntityTypeBuilder<BrevoConfiguration> builder)
    {
        builder.ToTable("brevo_configurations");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.EncryptedApiKey).HasMaxLength(1000);
        builder.Property(x => x.SenderName).HasMaxLength(200).IsRequired();
        builder.Property(x => x.SenderEmail).HasMaxLength(200).IsRequired();
        builder.Property(x => x.ReplyTo).HasMaxLength(200);
        builder.Property(x => x.TestModeEmail).HasMaxLength(200);
        builder.Property(x => x.WebhookSecret).HasMaxLength(500);

        builder.Property(x => x.DailyLimit).HasDefaultValue(300);
        builder.Property(x => x.IsEnabled).HasDefaultValue(false);
        builder.Property(x => x.TestMode).HasDefaultValue(false);
    }
}
