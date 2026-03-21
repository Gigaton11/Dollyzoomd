using DollyZoomd.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DollyZoomd.Data.Configurations;

public class CommentConfiguration : IEntityTypeConfiguration<Comment>
{
    public void Configure(EntityTypeBuilder<Comment> builder)
    {
        builder.ToTable("comments");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Text)
            .HasMaxLength(240)
            .IsRequired();

        builder.Property(x => x.CreatedAtUtc)
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.HasOne(x => x.Show)
            .WithMany(x => x.Comments)
            .HasForeignKey(x => x.ShowId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.User)
            .WithMany(x => x.Comments)
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => new { x.ShowId, x.CreatedAtUtc });
    }
}
