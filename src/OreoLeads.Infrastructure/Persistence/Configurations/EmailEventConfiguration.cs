using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OreoLeads.Domain.Entities;

namespace OreoLeads.Infrastructure.Persistence.Configurations;

public class EmailEventConfiguration : IEntityTypeConfiguration<EmailEvent>
{
    public void Configure(EntityTypeBuilder<EmailEvent> builder)
    {
        builder.ToTable("email_events");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.EventType)
            .HasConversion<string>()
            .HasMaxLength(30);

        builder.Property(x => x.Email).HasMaxLength(320);
        builder.Property(x => x.MessageId).HasMaxLength(500);
        builder.Property(x => x.IpAddress).HasMaxLength(45);
        // Details stored as raw JSON text — no max length to avoid truncation
        builder.Property(x => x.Details).HasColumnType("text");

        builder.HasIndex(x => new { x.LeadId, x.OccurredAt })
               .HasDatabaseName("ix_email_events_lead_occurred");

        builder.HasIndex(x => x.EmailSendJobId)
               .HasDatabaseName("ix_email_events_send_job_id");

        builder.HasIndex(x => x.MessageId)
               .HasDatabaseName("ix_email_events_message_id");
    }
}
