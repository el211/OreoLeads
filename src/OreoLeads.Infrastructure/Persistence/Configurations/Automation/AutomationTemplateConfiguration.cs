using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OreoLeads.Domain.Entities.Automation;

namespace OreoLeads.Infrastructure.Persistence.Configurations.Automation;

public class AutomationTemplateConfiguration : IEntityTypeConfiguration<AutomationTemplate>
{
    public void Configure(EntityTypeBuilder<AutomationTemplate> builder)
    {
        builder.ToTable("automation_templates");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Name).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Description).HasMaxLength(1000);
        builder.Property(x => x.Category).HasMaxLength(100);
        builder.Property(x => x.IconName).HasMaxLength(100);
        builder.Property(x => x.Tags).HasMaxLength(500);
    }
}
