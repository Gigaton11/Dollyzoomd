using Microsoft.EntityFrameworkCore;
using DollyZoomd.Models;

namespace DollyZoomd.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<Show> Shows => Set<Show>();
    public DbSet<WatchlistEntry> WatchlistEntries => Set<WatchlistEntry>();
    public DbSet<UserFavorite> UserFavorites => Set<UserFavorite>();
    public DbSet<DiscoverCache> DiscoverCaches => Set<DiscoverCache>();
    public DbSet<Comment> Comments => Set<Comment>();
    public DbSet<UserCommentVote> UserCommentVotes => Set<UserCommentVote>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}
