using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using DollyZoomd.Models;

namespace DollyZoomd.Data.Configurations;

public class DiscoverCacheConfiguration : IEntityTypeConfiguration<DiscoverCache>
{
    public void Configure(EntityTypeBuilder<DiscoverCache> builder)
    {
        // Composite primary key: (CategoryName, RankPosition) ensures unique ordering within each category
        builder.HasKey(dc => new { dc.CategoryName, dc.RankPosition });
        
        // Ensure CategoryName is not null and reasonably sized
        builder.Property(dc => dc.CategoryName)
            .IsRequired()
            .HasMaxLength(50);
        
        // RankPosition must be >= 0
        builder.Property(dc => dc.RankPosition)
            .IsRequired();
        
        // ShowId (FK) is required
        builder.Property(dc => dc.ShowId)
            .IsRequired();
        
        // Timestamps are required
        builder.Property(dc => dc.CachedAtUtc)
            .IsRequired()
            .HasColumnType("timestamp with time zone");
        
        builder.Property(dc => dc.ExpiryAtUtc)
            .IsRequired()
            .HasColumnType("timestamp with time zone");
        
        // Foreign key to Show with cascade delete (when a show is deleted, its discover cache rows are also deleted)
        builder.HasOne(dc => dc.Show)
            .WithMany()
            .HasForeignKey(dc => dc.ShowId)
            .OnDelete(DeleteBehavior.Cascade)
            .IsRequired();
        
        // Index on ExpiryAtUtc for efficient retrieval of expired entries during refresh
        builder.HasIndex(dc => dc.ExpiryAtUtc);
        
        // Index on (CategoryName, ExpiryAtUtc) to support fast retrieval and refresh checks per category
        builder.HasIndex(dc => new { dc.CategoryName, dc.ExpiryAtUtc });
    }
}
