using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OreoLeads.Domain.Entities.Automation;

namespace OreoLeads.Infrastructure.Persistence.Configurations.Automation;

public class AutomationWorkflowConfiguration : IEntityTypeConfiguration<AutomationWorkflow>
{
    public void Configure(EntityTypeBuilder<AutomationWorkflow> builder)
    {
        builder.ToTable("automation_workflows");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Name).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Description).HasMaxLength(1000);
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(50);
        builder.Property(x => x.Tags).HasMaxLength(500);
        builder.HasIndex(x => x.OrganizationId);
        builder.HasIndex(x => x.Status);
        builder.HasOne(x => x.Folder).WithMany(f => f.Workflows).HasForeignKey(x => x.FolderId).OnDelete(DeleteBehavior.SetNull);
    }
}
