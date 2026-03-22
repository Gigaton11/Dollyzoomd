using DollyZoomd.DTOs.Shows;
using DollyZoomd.Models;
using DollyZoomd.Options;
using DollyZoomd.Repositories.Interfaces;
using DollyZoomd.Services.Interfaces;
using Microsoft.Extensions.Options;

namespace DollyZoomd.Services;

public class CommentService(ICommentRepository commentRepository, IOptions<AvatarOptions> avatarOptions) : ICommentService
{
    private readonly AvatarOptions _avatarOptions = avatarOptions.Value;

    public async Task<CommentDto?> GetLatestCommentAsync(int showId, Guid? currentUserId, CancellationToken cancellationToken = default)
    {
        if (showId <= 0)
        {
            throw new ArgumentException("Show ID must be a positive integer.");
        }

        var comment = await commentRepository.GetLatestCommentByShowAsync(showId, cancellationToken);
        if (comment is null)
        {
            return null;
        }

        return MapComment(comment, currentUserId);
    }

    public async Task<CommentListDto> GetCommentsAsync(int showId, Guid? currentUserId, CancellationToken cancellationToken = default)
    {
        if (showId <= 0)
        {
            throw new ArgumentException("Show ID must be a positive integer.");
        }

        var comments = await commentRepository.GetCommentsByShowAsync(showId, cancellationToken);

        return new CommentListDto
        {
            TotalCount = comments.Count,
            Comments = comments.Select(comment => MapComment(comment, currentUserId)).ToList()
        };
    }

    public async Task<CommentDto> AddCommentAsync(Guid userId, int showId, AddCommentRequest request, CancellationToken cancellationToken = default)
    {
        if (showId <= 0)
        {
            throw new ArgumentException("Show ID must be a positive integer.");
        }

        var text = (request.Text ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(text))
        {
            throw new ArgumentException("Comment text is required.");
        }

        if (text.Length > 240)
        {
            throw new ArgumentException("Comments can be up to 240 characters.");
        }

        var showName = (request.ShowName ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(showName))
        {
            throw new ArgumentException("Show name is required.");
        }

        await commentRepository.UpsertShowCacheAsync(new Show
        {
            Id = showId,
            Name = showName,
            PosterUrl = request.PosterUrl,
            GenresCsv = request.GenresCsv
        }, cancellationToken);

        var comment = new Comment
        {
            ShowId = showId,
            UserId = userId,
            Text = text,
            CreatedAtUtc = DateTime.UtcNow
        };

        await commentRepository.AddCommentAsync(comment, cancellationToken);

        await commentRepository.AddVoteAsync(new UserCommentVote
        {
            CommentId = comment.Id,
            UserId = userId,
            IsUpvote = true
        }, cancellationToken);

        var created = await commentRepository.GetCommentByIdAsync(comment.Id, cancellationToken)
            ?? throw new InvalidOperationException("Could not load created comment.");

        return MapComment(created, userId);
    }

    public async Task<CommentDto> VoteCommentAsync(Guid userId, int showId, int commentId, bool isUpvote, CancellationToken cancellationToken = default)
    {
        var comment = await GetCommentForShowOrThrowAsync(showId, commentId, cancellationToken);
        EnsureCanVote(comment, userId);

        var vote = await commentRepository.GetVoteAsync(commentId, userId, cancellationToken);

        if (vote is null)
        {
            vote = new UserCommentVote
            {
                CommentId = commentId,
                UserId = userId,
                IsUpvote = isUpvote
            };

            await commentRepository.AddVoteAsync(vote, cancellationToken);
        }
        else if (vote.IsUpvote == isUpvote)
        {
            await commentRepository.DeleteVoteAsync(vote, cancellationToken);
        }
        else
        {
            vote.IsUpvote = isUpvote;
            await commentRepository.UpdateVoteAsync(vote, cancellationToken);
        }

        var refreshed = await commentRepository.GetCommentByIdAsync(commentId, cancellationToken)
            ?? throw new KeyNotFoundException("Comment not found.");

        return MapComment(refreshed, userId);
    }

    public async Task DeleteCommentAsync(Guid userId, int showId, int commentId, CancellationToken cancellationToken = default)
    {
        var comment = await GetCommentForShowOrThrowAsync(showId, commentId, cancellationToken);

        if (comment.UserId != userId)
        {
            throw new InvalidOperationException("You can only delete your own comments.");
        }

        await commentRepository.DeleteCommentAsync(comment, cancellationToken);
    }

    public async Task<CommentDto> RemoveVoteAsync(Guid userId, int showId, int commentId, CancellationToken cancellationToken = default)
    {
        var comment = await GetCommentForShowOrThrowAsync(showId, commentId, cancellationToken);
        EnsureCanVote(comment, userId);

        var vote = await commentRepository.GetVoteAsync(commentId, userId, cancellationToken);
        if (vote is not null)
        {
            await commentRepository.DeleteVoteAsync(vote, cancellationToken);
        }

        var refreshed = await commentRepository.GetCommentByIdAsync(commentId, cancellationToken)
            ?? throw new KeyNotFoundException("Comment not found.");

        return MapComment(refreshed, userId);
    }

    private async Task<Comment> GetCommentForShowOrThrowAsync(int showId, int commentId, CancellationToken cancellationToken)
    {
        if (showId <= 0)
        {
            throw new ArgumentException("Show ID must be a positive integer.");
        }

        if (commentId <= 0)
        {
            throw new ArgumentException("Comment ID must be a positive integer.");
        }

        var comment = await commentRepository.GetCommentByIdAsync(commentId, cancellationToken);
        if (comment is null || comment.ShowId != showId)
        {
            throw new KeyNotFoundException("Comment not found.");
        }

        return comment;
    }

    private static void EnsureCanVote(Comment comment, Guid userId)
    {
        if (comment.UserId == userId)
        {
            throw new InvalidOperationException("You cannot vote on your own comment.");
        }
    }

    private CommentDto MapComment(Comment comment, Guid? currentUserId)
    {
        var upvoteCount = comment.Votes.Count(v => v.IsUpvote);
        var downvoteCount = comment.Votes.Count(v => !v.IsUpvote);
        var currentVote = currentUserId is null
            ? null
            : comment.Votes
                .Where(v => v.UserId == currentUserId.Value)
                .Select(v => v.IsUpvote ? "upvote" : "downvote")
                .FirstOrDefault();

        return new CommentDto
        {
            Id = comment.Id,
            ShowId = comment.ShowId,
            Username = comment.User?.Username ?? "Unknown",
            AvatarUrl = BuildAvatarUrl(comment.User?.AvatarFileName),
            Text = comment.Text,
            CreatedAtUtc = comment.CreatedAtUtc,
            UpvoteCount = upvoteCount,
            DownvoteCount = downvoteCount,
            CurrentUserVote = currentVote,
            CanVote = currentUserId.HasValue && comment.UserId != currentUserId.Value,
            IsOwnedByCurrentUser = currentUserId.HasValue && comment.UserId == currentUserId.Value
        };
    }

    private string? BuildAvatarUrl(string? avatarFileName)
    {
        if (string.IsNullOrWhiteSpace(avatarFileName))
        {
            return null;
        }

        if (_avatarOptions.UseCloudStorage)
        {
            if (string.IsNullOrWhiteSpace(_avatarOptions.CloudStorageBucket))
            {
                return null;
            }

            return $"https://storage.googleapis.com/{_avatarOptions.CloudStorageBucket}/{avatarFileName}";
        }

        var normalizedStoragePath = NormalizeStoragePath(_avatarOptions.StoragePath);
        return $"/{normalizedStoragePath}/{avatarFileName}";
    }

    private static string NormalizeStoragePath(string? storagePath)
    {
        var normalized = (storagePath ?? "uploads/avatars")
            .Trim()
            .Replace('\\', '/');

        var trimmed = normalized.Trim('/');
        return string.IsNullOrWhiteSpace(trimmed) ? "uploads/avatars" : trimmed;
    }
}
