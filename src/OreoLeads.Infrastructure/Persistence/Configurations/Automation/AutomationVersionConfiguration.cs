using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OreoLeads.Domain.Entities.Automation;

namespace OreoLeads.Infrastructure.Persistence.Configurations.Automation;

public class AutomationVersionConfiguration : IEntityTypeConfiguration<AutomationVersion>
{
    public void Configure(EntityTypeBuilder<AutomationVersion> builder)
    {
        builder.ToTable("automation_versions");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Comment).HasMaxLength(500);
        builder.Property(x => x.CreatedBy).HasMaxLength(200);
        builder.HasIndex(x => x.WorkflowId);
        builder.HasOne(x => x.Workflow).WithMany(w => w.Versions).HasForeignKey(x => x.WorkflowId).OnDelete(DeleteBehavior.Cascade);
    }
}
