using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OreoLeads.Domain.Entities.Automation;

namespace OreoLeads.Infrastructure.Persistence.Configurations.Automation;

public class AutomationConditionConfiguration : IEntityTypeConfiguration<AutomationCondition>
{
    public void Configure(EntityTypeBuilder<AutomationCondition> builder)
    {
        builder.ToTable("automation_conditions");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.GroupId).HasMaxLength(100);
        builder.Property(x => x.Field).HasMaxLength(200);
        builder.Property(x => x.Operator).HasConversion<string>().HasMaxLength(50);
        builder.Property(x => x.LogicOperator).HasConversion<string>().HasMaxLength(20);
        builder.Property(x => x.Value).HasMaxLength(1000);
        builder.HasIndex(x => x.WorkflowId);
        builder.HasOne(x => x.Workflow).WithMany(w => w.Conditions).HasForeignKey(x => x.WorkflowId).OnDelete(DeleteBehavior.Cascade);
    }
}
