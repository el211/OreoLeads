using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OreoLeads.Domain.Entities;

namespace OreoLeads.Infrastructure.Persistence.Configurations;

public class SearchQueryConfiguration : IEntityTypeConfiguration<SearchQuery>
{
    public void Configure(EntityTypeBuilder<SearchQuery> builder)
    {
        builder.ToTable("search_queries");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Keywords).HasMaxLength(500);
        builder.Property(x => x.Region).HasMaxLength(100);
        builder.Property(x => x.Department).HasMaxLength(10);
        builder.Property(x => x.City).HasMaxLength(100);
        builder.Property(x => x.PostalCode).HasMaxLength(10);
        builder.Property(x => x.Industry).HasMaxLength(100);
        builder.Property(x => x.NafCode).HasMaxLength(10);
        builder.Property(x => x.Provider).HasMaxLength(50).HasDefaultValue("OpenDataGouv");
        builder.Property(x => x.Status).HasMaxLength(50).HasDefaultValue("Searched");
        builder.Property(x => x.UserId).HasMaxLength(100);

        builder.HasIndex(x => x.CreatedAt);
    }
}
