using DollyZoomd.DTOs.Favorites;

namespace DollyZoomd.DTOs.Profile;

public class UserProfileDto
{
    public string Username { get; set; } = string.Empty;
    public DateTime MemberSinceUtc { get; set; }
    public WatchlistSummaryDto WatchlistSummary { get; set; } = new();
    public IReadOnlyList<FavoriteDto> Favorites { get; set; } = [];
}
