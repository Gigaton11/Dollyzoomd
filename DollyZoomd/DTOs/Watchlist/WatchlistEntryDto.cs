using DollyZoomd.Models.Enums;

namespace DollyZoomd.DTOs.Watchlist;

public class WatchlistEntryDto
{
    public int ShowId { get; set; }
    public string ShowName { get; set; } = string.Empty;
    public string? PosterUrl { get; set; }
    public IReadOnlyList<string> Genres { get; set; } = Array.Empty<string>();
    public WatchStatus Status { get; set; }
    public int? Rating { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
}
