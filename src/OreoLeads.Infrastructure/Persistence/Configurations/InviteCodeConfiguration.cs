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

        builder.Property(x => x.Code).IsRequired();
        builder.Property(x => x.IsUsed).HasDefaultValue(false);

        builder.HasIndex(x => x.Code).IsUnique();
    }
}
