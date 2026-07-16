using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OreoLeads.Domain.Entities;
using OreoLeads.Domain.Enums;

namespace OreoLeads.Infrastructure.Persistence.Configurations;

public class FollowUpConfiguration : IEntityTypeConfiguration<FollowUp>
{
    public void Configure(EntityTypeBuilder<FollowUp> builder)
    {
        builder.ToTable("follow_ups");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.UserName).HasMaxLength(100);
        builder.Property(x => x.Comment).HasMaxLength(1000);

        builder.Property(x => x.Status)
            .HasConversion<string>()
            .HasMaxLength(20)
            .HasDefaultValue(FollowUpStatus.Pending);

        builder.Property(x => x.Priority)
            .HasConversion<string>()
            .HasMaxLength(20)
            .HasDefaultValue(LeadPriority.Medium);

        builder.HasIndex(x => new { x.ScheduledAt, x.Status });
        builder.HasIndex(x => x.LeadId);
    }
}
