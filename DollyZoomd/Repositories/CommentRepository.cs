using DollyZoomd.Data;
using DollyZoomd.Models;
using DollyZoomd.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace DollyZoomd.Repositories;

public class CommentRepository(AppDbContext dbContext) : ICommentRepository
{
    public Task<Comment?> GetLatestCommentByShowAsync(int showId, CancellationToken cancellationToken = default)
    {
        return dbContext.Comments
            .Where(x => x.ShowId == showId)
            .Include(x => x.User)
            .Include(x => x.Votes)
            .OrderByDescending(x => x.CreatedAtUtc)
            .ThenByDescending(x => x.Id)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Comment>> GetCommentsByShowAsync(int showId, CancellationToken cancellationToken = default)
    {
        return await dbContext.Comments
            .Where(x => x.ShowId == showId)
            .Include(x => x.User)
            .Include(x => x.Votes)
            .OrderByDescending(x => x.CreatedAtUtc)
            .ThenByDescending(x => x.Id)
            .ToListAsync(cancellationToken);
    }

    public Task<Comment?> GetCommentByIdAsync(int commentId, CancellationToken cancellationToken = default)
    {
        return dbContext.Comments
            .Include(x => x.User)
            .Include(x => x.Votes)
            .SingleOrDefaultAsync(x => x.Id == commentId, cancellationToken);
    }

    public Task<UserCommentVote?> GetVoteAsync(int commentId, Guid userId, CancellationToken cancellationToken = default)
    {
        return dbContext.UserCommentVotes
            .SingleOrDefaultAsync(x => x.CommentId == commentId && x.UserId == userId, cancellationToken);
    }

    public async Task AddCommentAsync(Comment comment, CancellationToken cancellationToken = default)
    {
        await dbContext.Comments.AddAsync(comment, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task AddVoteAsync(UserCommentVote vote, CancellationToken cancellationToken = default)
    {
        await dbContext.UserCommentVotes.AddAsync(vote, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateVoteAsync(UserCommentVote vote, CancellationToken cancellationToken = default)
    {
        dbContext.UserCommentVotes.Update(vote);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteVoteAsync(UserCommentVote vote, CancellationToken cancellationToken = default)
    {
        dbContext.UserCommentVotes.Remove(vote);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteCommentAsync(Comment comment, CancellationToken cancellationToken = default)
    {
        dbContext.Comments.Remove(comment);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpsertShowCacheAsync(Show show, CancellationToken cancellationToken = default)
    {
        var existing = await dbContext.Shows.FindAsync([show.Id], cancellationToken);
        if (existing is null)
        {
            await dbContext.Shows.AddAsync(show, cancellationToken);
        }
        else
        {
            CopyShowCacheFields(show, existing);
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static void CopyShowCacheFields(Show source, Show target)
    {
        target.Name = source.Name;
        target.PosterUrl = source.PosterUrl;
        target.GenresCsv = source.GenresCsv;
        target.CachedAtUtc = DateTime.UtcNow;
    }
}
