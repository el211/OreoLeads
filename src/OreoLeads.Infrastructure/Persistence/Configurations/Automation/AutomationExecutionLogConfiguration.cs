using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OreoLeads.Domain.Entities.Automation;

namespace OreoLeads.Infrastructure.Persistence.Configurations.Automation;

public class AutomationExecutionLogConfiguration : IEntityTypeConfiguration<AutomationExecutionLog>
{
    public void Configure(EntityTypeBuilder<AutomationExecutionLog> builder)
    {
        builder.ToTable("automation_execution_logs");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.ActionName).HasMaxLength(200);
        builder.Property(x => x.Level).HasMaxLength(20);
        builder.HasIndex(x => x.ExecutionId);
        builder.HasOne(x => x.Execution).WithMany(e => e.Logs).HasForeignKey(x => x.ExecutionId).OnDelete(DeleteBehavior.Cascade);
    }
}
