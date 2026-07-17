using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OreoLeads.Domain.Entities.Automation;

namespace OreoLeads.Infrastructure.Persistence.Configurations.Automation;

public class AutomationTriggerConfiguration : IEntityTypeConfiguration<AutomationTrigger>
{
    public void Configure(EntityTypeBuilder<AutomationTrigger> builder)
    {
        builder.ToTable("automation_triggers");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Type).HasConversion<string>().HasMaxLength(50);
        builder.HasIndex(x => x.WorkflowId);
        builder.HasOne(x => x.Workflow).WithMany(w => w.Triggers).HasForeignKey(x => x.WorkflowId).OnDelete(DeleteBehavior.Cascade);
    }
}
