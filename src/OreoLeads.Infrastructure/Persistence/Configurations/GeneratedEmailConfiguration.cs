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
        builder.Property(x => x.Subject).IsRequired().HasMaxLength(200);
        builder.Property(x => x.Body).IsRequired();
        builder.Property(x => x.Status)
            .HasConversion<string>()
            .HasMaxLength(20)
            .HasDefaultValue(EmailStatus.Draft);
        builder.HasIndex(x => x.LeadId);
    }
}
