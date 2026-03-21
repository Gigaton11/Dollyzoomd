using DollyZoomd.DTOs.Shows;

namespace DollyZoomd.Services.Interfaces;

public interface ICommentService
{
    Task<CommentDto?> GetLatestCommentAsync(int showId, Guid? currentUserId, CancellationToken cancellationToken = default);
    Task<CommentListDto> GetCommentsAsync(int showId, Guid? currentUserId, CancellationToken cancellationToken = default);
    Task<CommentDto> AddCommentAsync(Guid userId, int showId, AddCommentRequest request, CancellationToken cancellationToken = default);
    Task<CommentDto> VoteCommentAsync(Guid userId, int showId, int commentId, bool isUpvote, CancellationToken cancellationToken = default);
    Task<CommentDto> RemoveVoteAsync(Guid userId, int showId, int commentId, CancellationToken cancellationToken = default);
    Task DeleteCommentAsync(Guid userId, int showId, int commentId, CancellationToken cancellationToken = default);
}
