using System.Security.Claims;
using DollyZoomd.DTOs.Watchlist;
using DollyZoomd.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DollyZoomd.Controllers;

[ApiController]
[Route("api/watchlist")]
[Authorize]
public class WatchlistController(IWatchlistService watchlistService) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<WatchlistEntryDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<WatchlistEntryDto>>> GetMyWatchlist(CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        var entries = await watchlistService.GetWatchlistAsync(userId, cancellationToken);
        return Ok(entries);
    }

    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> AddToWatchlist([FromBody] AddToWatchlistRequest request, CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        await watchlistService.AddToWatchlistAsync(userId, request, cancellationToken);
        return StatusCode(StatusCodes.Status201Created);
    }

    [HttpPut("{showId:int}/status")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateStatus(int showId, [FromBody] UpdateWatchStatusRequest request, CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        await watchlistService.UpdateStatusAsync(userId, showId, request, cancellationToken);
        return NoContent();
    }

    [HttpPut("{showId:int}/rating")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RateShow(int showId, [FromBody] RateShowRequest request, CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        await watchlistService.RateShowAsync(userId, showId, request, cancellationToken);
        return NoContent();
    }

    [HttpDelete("{showId:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RemoveFromWatchlist(int showId, CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        await watchlistService.RemoveFromWatchlistAsync(userId, showId, cancellationToken);
        return NoContent();
    }

    private Guid GetUserId()
    {
        var claim = User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? throw new UnauthorizedAccessException("User identity not found in token.");

        if (!Guid.TryParse(claim, out var userId))
        {
            throw new UnauthorizedAccessException("User identity in token is invalid.");
        }

        return userId;
    }
}
