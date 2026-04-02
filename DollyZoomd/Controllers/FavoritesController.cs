using System.Security.Claims;
using DollyZoomd.DTOs.Favorites;
using DollyZoomd.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DollyZoomd.Controllers;

[ApiController]
[Route("api/favorites")]
[Authorize]
public class FavoritesController(IFavoritesService favoritesService) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<FavoriteDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<FavoriteDto>>> GetFavorites(CancellationToken cancellationToken)
    {
        // All favorites endpoints are user-scoped via JWT identity.
        var userId = GetUserId();
        var favorites = await favoritesService.GetFavoritesAsync(userId, cancellationToken);
        return Ok(favorites);
    }

    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> AddFavorite([FromBody] AddFavoriteRequest request, CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        await favoritesService.AddFavoriteAsync(userId, request, cancellationToken);
        return StatusCode(StatusCodes.Status201Created);
    }

    [HttpDelete("{showId:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RemoveFavorite(int showId, CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        await favoritesService.RemoveFavoriteAsync(userId, showId, cancellationToken);
        return NoContent();
    }

    private Guid GetUserId()
    {
        // NameIdentifier is generated in JWT creation and treated as the canonical user key.
        var claim = User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? throw new UnauthorizedAccessException("User identity not found in token.");

        if (!Guid.TryParse(claim, out var userId))
        {
            throw new UnauthorizedAccessException("User identity in token is invalid.");
        }

        return userId;
    }
}
