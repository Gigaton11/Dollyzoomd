using DollyZoomd.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DollyZoomd.Data.Configurations;

public class ShowConfiguration : IEntityTypeConfiguration<Show>
{
    public void Configure(EntityTypeBuilder<Show> builder)
    {
        builder.ToTable("shows");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .ValueGeneratedNever();

        builder.Property(x => x.Name)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(x => x.PosterUrl)
            .HasMaxLength(500);

        builder.Property(x => x.GenresCsv)
            .HasMaxLength(500);

        builder.Property(x => x.CachedAtUtc)
            .HasColumnType("timestamp with time zone")
            .IsRequired();
    }
}
