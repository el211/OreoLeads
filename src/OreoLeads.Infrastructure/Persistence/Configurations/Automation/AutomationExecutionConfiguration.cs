using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OreoLeads.Domain.Entities.Automation;

namespace OreoLeads.Infrastructure.Persistence.Configurations.Automation;

public class AutomationExecutionConfiguration : IEntityTypeConfiguration<AutomationExecution>
{
    public void Configure(EntityTypeBuilder<AutomationExecution> builder)
    {
        builder.ToTable("automation_executions");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.WorkflowName).HasMaxLength(200);
        builder.Property(x => x.TriggerType).HasConversion<string>().HasMaxLength(50);
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(50);
        builder.HasIndex(x => x.WorkflowId);
        builder.HasIndex(x => x.OrganizationId);
        builder.HasIndex(x => x.Status);
        builder.HasOne(x => x.Workflow).WithMany(w => w.Executions).HasForeignKey(x => x.WorkflowId).OnDelete(DeleteBehavior.Cascade);
    }
}
