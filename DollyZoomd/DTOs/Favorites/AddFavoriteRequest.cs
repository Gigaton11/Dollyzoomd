using System.ComponentModel.DataAnnotations;

namespace DollyZoomd.DTOs.Favorites;

public class AddFavoriteRequest
{
    [Required]
    public int TvMazeShowId { get; set; }

    [Required]
    [MaxLength(200)]
    public string ShowName { get; set; } = string.Empty;

    public string? PosterUrl { get; set; }
    public string? GenresCsv { get; set; }
}
