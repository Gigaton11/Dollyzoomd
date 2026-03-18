using DollyZoomd.Models.Enums;

namespace DollyZoomd.Models;

public class WatchlistEntry
{
    public Guid UserId { get; set; }
    public int ShowId { get; set; }
    public WatchStatus Status { get; set; } = WatchStatus.PlanToWatch;
    public int? Rating { get; set; }
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;

    public User? User { get; set; }
    public Show? Show { get; set; }
}
