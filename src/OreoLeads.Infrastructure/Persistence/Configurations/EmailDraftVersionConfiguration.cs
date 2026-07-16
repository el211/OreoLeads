using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OreoLeads.Domain.Entities;

namespace OreoLeads.Infrastructure.Persistence.Configurations;

public class EmailDraftVersionConfiguration : IEntityTypeConfiguration<EmailDraftVersion>
{
    public void Configure(EntityTypeBuilder<EmailDraftVersion> builder)
    {
        builder.ToTable("email_draft_versions");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Subject).IsRequired().HasMaxLength(300);
        builder.Property(x => x.Body).IsRequired();
        builder.Property(x => x.ProviderUsed).HasMaxLength(50);
        builder.Property(x => x.ModelUsed).HasMaxLength(100);

        builder.HasIndex(x => x.EmailDraftId);
        builder.HasIndex(x => new { x.EmailDraftId, x.Version }).IsUnique();
    }
}
