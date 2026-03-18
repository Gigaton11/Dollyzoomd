using System.ComponentModel.DataAnnotations;

namespace DollyZoomd.DTOs.Watchlist;

public class RateShowRequest
{
    [Required]
    [Range(1, 10)]
    public int Rating { get; set; }
}
