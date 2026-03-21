namespace DollyZoomd.DTOs.Shows;

public class CommentDto
{
    public int Id { get; set; }
    public int ShowId { get; set; }
    public string Username { get; set; } = string.Empty;
    public string? AvatarUrl { get; set; }
    public string Text { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; }
    public int UpvoteCount { get; set; }
    public int DownvoteCount { get; set; }
    public string? CurrentUserVote { get; set; }
    public bool CanVote { get; set; }
    public bool IsOwnedByCurrentUser { get; set; }
}
