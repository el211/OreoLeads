using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OreoLeads.Domain.Entities;

namespace OreoLeads.Infrastructure.Persistence.Configurations;

public class InviteCodeConfiguration : IEntityTypeConfiguration<InviteCode>
{
    public void Configure(EntityTypeBuilder<InviteCode> builder)
    {
        builder.ToTable("invite_codes");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.CreatedAt).HasColumnName("created_at");
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at");

        builder.Property(x => x.Code).HasColumnName("code").IsRequired();
        builder.Property(x => x.Note).HasColumnName("note");
        builder.Property(x => x.IsUsed).HasColumnName("is_used").HasDefaultValue(false);
        builder.Property(x => x.UsedByEmail).HasColumnName("used_by_email");
        builder.Property(x => x.UsedAt).HasColumnName("used_at");
        builder.Property(x => x.ExpiresAt).HasColumnName("expires_at");

        builder.HasIndex(x => x.Code).IsUnique();
    }
}
