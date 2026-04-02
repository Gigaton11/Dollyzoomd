using System.Security.Claims;
using DollyZoomd.DTOs.Shows;
using DollyZoomd.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DollyZoomd.Controllers;

[ApiController]
[Route("api/shows/{showId:int}/comments")]
public class CommentsController(ICommentService commentService) : ControllerBase
{
    [HttpGet("latest")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(CommentDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CommentDto?>> GetLatest(int showId, CancellationToken cancellationToken)
    {
        // Anonymous users are allowed; user context is optional for vote ownership flags.
        var currentUserId = TryGetUserId();
        var latestComment = await commentService.GetLatestCommentAsync(showId, currentUserId, cancellationToken);
        return Ok(latestComment);
    }

    [HttpGet]
    [AllowAnonymous]
    [ProducesResponseType(typeof(CommentListDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<CommentListDto>> GetAll(int showId, CancellationToken cancellationToken)
    {
        // Include optional user context so each row can indicate current user's vote/ownership.
        var currentUserId = TryGetUserId();
        var comments = await commentService.GetCommentsAsync(showId, currentUserId, cancellationToken);
        return Ok(comments);
    }

    [HttpPost]
    [Authorize]
    [ProducesResponseType(typeof(CommentDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<CommentDto>> AddComment(int showId, [FromBody] AddCommentRequest request, CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        var created = await commentService.AddCommentAsync(userId, showId, request, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, created);
    }

    [HttpPut("{commentId:int}/vote")]
    [Authorize]
    [ProducesResponseType(typeof(CommentDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CommentDto>> Vote(int showId, int commentId, [FromBody] VoteCommentRequest request, CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        var updated = await commentService.VoteCommentAsync(userId, showId, commentId, request.IsUpvote, cancellationToken);
        return Ok(updated);
    }

    [HttpDelete("{commentId:int}/vote")]
    [Authorize]
    [ProducesResponseType(typeof(CommentDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CommentDto>> RemoveVote(int showId, int commentId, CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        var updated = await commentService.RemoveVoteAsync(userId, showId, commentId, cancellationToken);
        return Ok(updated);
    }

    [HttpDelete("{commentId:int}")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteComment(int showId, int commentId, CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        await commentService.DeleteCommentAsync(userId, showId, commentId, cancellationToken);
        return NoContent();
    }

    private Guid GetUserId()
    {
        return GetUserIdCore(requireValidGuid: true)
            ?? throw new UnauthorizedAccessException("User identity not found in token.");
    }

    private Guid? TryGetUserId()
    {
        // Anonymous endpoints allow missing identity; malformed IDs are treated as absent.
        return GetUserIdCore(requireValidGuid: false);
    }

    private Guid? GetUserIdCore(bool requireValidGuid)
    {
        var claim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(claim))
        {
            return null;
        }

        if (Guid.TryParse(claim, out var userId))
        {
            return userId;
        }

        if (requireValidGuid)
        {
            throw new UnauthorizedAccessException("User identity in token is invalid.");
        }

        return null;
    }
}
