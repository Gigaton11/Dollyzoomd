namespace DollyZoomd.Models;

public class Show
{
    // TVMaze show ID is used as the primary key for cached shows.
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? PosterUrl { get; set; }
    public string? GenresCsv { get; set; }
    public DateOnly? PremieredOn { get; set; }
    public double? AverageRating { get; set; }
    public DateTime CachedAtUtc { get; set; } = DateTime.UtcNow;

    public ICollection<WatchlistEntry> WatchlistEntries { get; set; } = new List<WatchlistEntry>();
    public ICollection<UserFavorite> FavoritedBy { get; set; } = new List<UserFavorite>();
}
