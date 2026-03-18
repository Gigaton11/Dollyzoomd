namespace DollyZoomd.DTOs.Shows;

public class ShowSearchItemDto
{
    public int TvMazeId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? PosterUrl { get; set; }
    public IReadOnlyList<string> Genres { get; set; } = Array.Empty<string>();
    public DateOnly? PremieredOn { get; set; }
    public double? AverageRating { get; set; }
}
