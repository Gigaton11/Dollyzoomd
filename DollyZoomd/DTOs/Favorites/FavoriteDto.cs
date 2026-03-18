namespace DollyZoomd.DTOs.Favorites;

public class FavoriteDto
{
    public int ShowId { get; set; }
    public string ShowName { get; set; } = string.Empty;
    public string? PosterUrl { get; set; }
    public IReadOnlyList<string> Genres { get; set; } = Array.Empty<string>();
    public int DisplayOrder { get; set; }
}
