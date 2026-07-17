using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OreoLeads.Domain.Entities.Automation;

namespace OreoLeads.Infrastructure.Persistence.Configurations.Automation;

public class AutomationExecutionErrorConfiguration : IEntityTypeConfiguration<AutomationExecutionError>
{
    public void Configure(EntityTypeBuilder<AutomationExecutionError> builder)
    {
        builder.ToTable("automation_execution_errors");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.ErrorType).HasMaxLength(200);
        builder.Property(x => x.ActionName).HasMaxLength(200);
        builder.HasIndex(x => x.ExecutionId);
        builder.HasOne(x => x.Execution).WithMany(e => e.Errors).HasForeignKey(x => x.ExecutionId).OnDelete(DeleteBehavior.Cascade);
    }
}
