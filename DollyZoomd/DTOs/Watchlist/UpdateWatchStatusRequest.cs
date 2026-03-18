using System.ComponentModel.DataAnnotations;
using DollyZoomd.Models.Enums;

namespace DollyZoomd.DTOs.Watchlist;

public class UpdateWatchStatusRequest
{
    [Required]
    public WatchStatus Status { get; set; }
}
