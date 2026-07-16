using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OreoLeads.Domain.Entities;

namespace OreoLeads.Infrastructure.Persistence.Configurations;

public class PromptTemplateConfiguration : IEntityTypeConfiguration<PromptTemplate>
{
    public void Configure(EntityTypeBuilder<PromptTemplate> builder)
    {
        builder.ToTable("prompt_templates");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Name).IsRequired().HasMaxLength(100);
        builder.Property(x => x.Key).IsRequired().HasMaxLength(50);
        builder.Property(x => x.Content).IsRequired();
        builder.Property(x => x.Description).HasMaxLength(500);

        builder.Property(x => x.EmailType)
            .HasConversion<string?>()
            .HasMaxLength(20);

        builder.HasIndex(x => x.Key).IsUnique();
    }
}
