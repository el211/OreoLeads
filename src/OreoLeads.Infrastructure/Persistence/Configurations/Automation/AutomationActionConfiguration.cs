using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OreoLeads.Domain.Entities.Automation;

namespace OreoLeads.Infrastructure.Persistence.Configurations.Automation;

public class AutomationActionConfiguration : IEntityTypeConfiguration<AutomationAction>
{
    public void Configure(EntityTypeBuilder<AutomationAction> builder)
    {
        builder.ToTable("automation_actions");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Name).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Type).HasConversion<string>().HasMaxLength(50);
        builder.HasIndex(x => x.WorkflowId);
        builder.HasOne(x => x.Workflow).WithMany(w => w.Actions).HasForeignKey(x => x.WorkflowId).OnDelete(DeleteBehavior.Cascade);
    }
}
