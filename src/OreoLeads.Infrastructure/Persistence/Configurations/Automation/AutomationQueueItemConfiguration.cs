using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OreoLeads.Domain.Entities.Automation;

namespace OreoLeads.Infrastructure.Persistence.Configurations.Automation;

public class AutomationQueueItemConfiguration : IEntityTypeConfiguration<AutomationQueueItem>
{
    public void Configure(EntityTypeBuilder<AutomationQueueItem> builder)
    {
        builder.ToTable("automation_queue_items");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(50);
        builder.Property(x => x.LockedBy).HasMaxLength(200);
        builder.HasIndex(x => new { x.Status, x.ScheduledAt });
        builder.HasIndex(x => x.WorkflowId);
        builder.HasIndex(x => x.OrganizationId);
    }
}
