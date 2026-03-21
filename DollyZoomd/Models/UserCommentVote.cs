namespace DollyZoomd.Models;

public class UserCommentVote
{
    public int Id { get; set; }
    public int CommentId { get; set; }
    public Guid UserId { get; set; }
    public bool IsUpvote { get; set; }

    public Comment? Comment { get; set; }
    public User? User { get; set; }
}
