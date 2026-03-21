namespace DollyZoomd.Models;

public class Comment
{
    public int Id { get; set; }
    public int ShowId { get; set; }
    public Guid UserId { get; set; }
    public string Text { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public Show? Show { get; set; }
    public User? User { get; set; }
    public ICollection<UserCommentVote> Votes { get; set; } = new List<UserCommentVote>();
}
