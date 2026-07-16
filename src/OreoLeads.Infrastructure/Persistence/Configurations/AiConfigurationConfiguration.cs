using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OreoLeads.Domain.Entities;
using OreoLeads.Domain.Enums;

namespace OreoLeads.Infrastructure.Persistence.Configurations;

public class AiConfigurationConfiguration : IEntityTypeConfiguration<AiConfiguration>
{
    public void Configure(EntityTypeBuilder<AiConfiguration> builder)
    {
        builder.ToTable("ai_configurations");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.ProviderType)
            .HasConversion<string>()
            .HasMaxLength(20)
            .HasDefaultValue(AiProviderType.Claude);

        builder.Property(x => x.EncryptedApiKey).HasMaxLength(500);
        builder.Property(x => x.Model).HasMaxLength(100);
        builder.Property(x => x.BaseUrl).HasMaxLength(200);
    }
}
