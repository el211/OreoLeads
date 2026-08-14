using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OreoLeads.Domain.Entities;

namespace OreoLeads.Infrastructure.Persistence.Configurations;

public class ChatMessageConfiguration : IEntityTypeConfiguration<ChatMessage>
{
    public void Configure(EntityTypeBuilder<ChatMessage> builder)
    {
        builder.ToTable("chat_messages");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.UserId).IsRequired();
        builder.Property(x => x.AuthorName).IsRequired().HasMaxLength(100);
        builder.Property(x => x.Content).IsRequired().HasMaxLength(2000);

        builder.HasIndex(x => x.CreatedAt);
    }
}
