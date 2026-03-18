namespace DollyZoomd.Models;

public class UserFavorite
{
    public Guid UserId { get; set; }
    public int ShowId { get; set; }
    public int DisplayOrder { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public User? User { get; set; }
    public Show? Show { get; set; }
}
