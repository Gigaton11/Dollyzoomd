using System.ComponentModel.DataAnnotations;
using DollyZoomd.Models.Enums;

namespace DollyZoomd.DTOs.Watchlist;

public class AddToWatchlistRequest
{
    [Required]
    public int TvMazeShowId { get; set; }

    [Required]
    [MaxLength(200)]
    public string ShowName { get; set; } = string.Empty;

    public string? PosterUrl { get; set; }
    public string? GenresCsv { get; set; }

    public WatchStatus Status { get; set; } = WatchStatus.PlanToWatch;
}
