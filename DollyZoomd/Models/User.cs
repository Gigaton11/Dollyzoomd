namespace DollyZoomd.Models;

public class User
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public ICollection<WatchlistEntry> WatchlistEntries { get; set; } = new List<WatchlistEntry>();
    public ICollection<UserFavorite> Favorites { get; set; } = new List<UserFavorite>();
}
