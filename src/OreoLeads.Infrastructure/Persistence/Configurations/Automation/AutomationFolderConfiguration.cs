using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OreoLeads.Domain.Entities.Automation;

namespace OreoLeads.Infrastructure.Persistence.Configurations.Automation;

public class AutomationFolderConfiguration : IEntityTypeConfiguration<AutomationFolder>
{
    public void Configure(EntityTypeBuilder<AutomationFolder> builder)
    {
        builder.ToTable("automation_folders");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Name).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Description).HasMaxLength(500);
        builder.Property(x => x.Color).HasMaxLength(20);
        builder.Property(x => x.Icon).HasMaxLength(100);
        builder.HasIndex(x => x.OrganizationId);
    }
}
