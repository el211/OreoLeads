using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OreoLeads.Domain.Entities.Automation;

namespace OreoLeads.Infrastructure.Persistence.Configurations.Automation;

public class AutomationScheduleConfiguration : IEntityTypeConfiguration<AutomationSchedule>
{
    public void Configure(EntityTypeBuilder<AutomationSchedule> builder)
    {
        builder.ToTable("automation_schedules");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Interval).HasConversion<string>().HasMaxLength(50);
        builder.Property(x => x.CronExpression).HasMaxLength(100);
        builder.Property(x => x.Timezone).HasMaxLength(100);
        builder.HasIndex(x => new { x.IsEnabled, x.NextRunAt });
        builder.HasIndex(x => x.WorkflowId);
        builder.HasOne(x => x.Workflow).WithMany(w => w.Schedules).HasForeignKey(x => x.WorkflowId).OnDelete(DeleteBehavior.Cascade);
    }
}
