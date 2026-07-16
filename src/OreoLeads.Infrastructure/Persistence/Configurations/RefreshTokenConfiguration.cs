using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OreoLeads.Domain.Entities;

namespace OreoLeads.Infrastructure.Persistence.Configurations;

public class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
        builder.HasKey(r => r.Id);
        builder.Property(r => r.Token).IsRequired().HasMaxLength(500);
        builder.Property(r => r.UserId).IsRequired().HasMaxLength(450);
        builder.Property(r => r.IpAddress).HasMaxLength(45);
        builder.Property(r => r.UserAgent).HasMaxLength(500);
        builder.Property(r => r.ReplacedByToken).HasMaxLength(500);

        builder.HasIndex(r => r.Token).IsUnique();
        builder.HasIndex(r => r.UserId);
    }
}
