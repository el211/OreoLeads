using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OreoLeads.Domain.Entities;
using OreoLeads.Domain.Enums;

namespace OreoLeads.Infrastructure.Persistence.Configurations;

public class EmailSendJobConfiguration : IEntityTypeConfiguration<EmailSendJob>
{
    public void Configure(EntityTypeBuilder<EmailSendJob> builder)
    {
        builder.ToTable("email_send_jobs");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Status)
            .HasConversion<string>()
            .HasMaxLength(20)
            .HasDefaultValue(EmailSendStatus.Pending);

        builder.Property(x => x.ToEmail).HasMaxLength(320).IsRequired();
        builder.Property(x => x.ToName).HasMaxLength(200);
        builder.Property(x => x.Subject).HasMaxLength(998).IsRequired();
        builder.Property(x => x.ErrorMessage).HasMaxLength(2000);
        builder.Property(x => x.BrevoMessageId).HasMaxLength(500);
        builder.Property(x => x.AttemptCount).HasDefaultValue(0);
        builder.Property(x => x.MaxAttempts).HasDefaultValue(3);

        // Composite index for queue polling
        builder.HasIndex(x => new { x.Status, x.ScheduledAt, x.NextAttemptAt })
               .HasDatabaseName("ix_email_send_jobs_status_scheduled");

        builder.HasIndex(x => x.GeneratedEmailId)
               .HasDatabaseName("ix_email_send_jobs_generated_email_id");

        builder.HasIndex(x => x.LeadId)
               .HasDatabaseName("ix_email_send_jobs_lead_id");

        builder.HasIndex(x => x.BrevoMessageId)
               .HasDatabaseName("ix_email_send_jobs_brevo_message_id");
    }
}
