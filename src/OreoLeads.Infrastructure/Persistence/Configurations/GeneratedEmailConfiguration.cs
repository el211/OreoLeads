using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OreoLeads.Domain.Entities;
using OreoLeads.Domain.Enums;

namespace OreoLeads.Infrastructure.Persistence.Configurations;

public class GeneratedEmailConfiguration : IEntityTypeConfiguration<GeneratedEmail>
{
    public void Configure(EntityTypeBuilder<GeneratedEmail> builder)
    {
        builder.ToTable("generated_emails");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Subject).IsRequired().HasMaxLength(300);
        builder.Property(x => x.Body).IsRequired();
        builder.Property(x => x.Summary).HasMaxLength(500);
        builder.Property(x => x.CallToAction).HasMaxLength(300);

        builder.Property(x => x.Status)
            .HasConversion<string>()
            .HasMaxLength(20)
            .HasDefaultValue(EmailStatus.Generated);

        builder.Property(x => x.Style)
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(x => x.Length)
            .HasConversion<string>()
            .HasMaxLength(10);

        builder.Property(x => x.Type)
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(x => x.ProviderUsed).HasMaxLength(50);
        builder.Property(x => x.ModelUsed).HasMaxLength(100);

        builder.HasIndex(x => x.LeadId);
        builder.HasIndex(x => x.Status);
        builder.HasIndex(x => x.CreatedAt);

        builder.HasMany(x => x.Versions)
            .WithOne(x => x.EmailDraft)
            .HasForeignKey(x => x.EmailDraftId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
