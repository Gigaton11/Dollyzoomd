using DollyZoomd.Models;

namespace DollyZoomd.Repositories.Interfaces;

public interface ICommentRepository
{
    Task<Comment?> GetLatestCommentByShowAsync(int showId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Comment>> GetCommentsByShowAsync(int showId, CancellationToken cancellationToken = default);
    Task<Comment?> GetCommentByIdAsync(int commentId, CancellationToken cancellationToken = default);
    Task<UserCommentVote?> GetVoteAsync(int commentId, Guid userId, CancellationToken cancellationToken = default);
    Task AddCommentAsync(Comment comment, CancellationToken cancellationToken = default);
    Task AddVoteAsync(UserCommentVote vote, CancellationToken cancellationToken = default);
    Task UpdateVoteAsync(UserCommentVote vote, CancellationToken cancellationToken = default);
    Task DeleteVoteAsync(UserCommentVote vote, CancellationToken cancellationToken = default);
    Task DeleteCommentAsync(Comment comment, CancellationToken cancellationToken = default);
    Task UpsertShowCacheAsync(Show show, CancellationToken cancellationToken = default);
}
