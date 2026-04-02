using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using DollyZoomd.DTOs.Shows;
using DollyZoomd.Services.Interfaces;

namespace DollyZoomd.Controllers;

[ApiController]
[Route("api/[controller]")]
[AllowAnonymous]
public class DiscoverController(IDiscoverService discoverService) : ControllerBase
{
    private const int DefaultTake = 20;
    private const int MaxTake = 100;
    private readonly IDiscoverService _discoverService = discoverService;

    /// <summary>
    /// Retrieves popular shows for the "Popular Right Now" carousel.
    /// No authentication required. Implements lazy cache refresh.
    /// </summary>
    /// <param name="take">Maximum number of shows to return (defaults to 20)</param>
    /// <param name="skip">Number of shows to skip for pagination (defaults to 0)</param>
    /// <returns>List of popular shows</returns>
    [HttpGet("popular")]
    public async Task<ActionResult<IReadOnlyList<ShowSearchItemDto>>> GetPopular([FromQuery] int take = 20, [FromQuery] int skip = 0)
    {
        var (normalizedTake, normalizedSkip) = NormalizePaging(take, skip);

        var shows = await _discoverService.GetPopularShowsAsync(normalizedTake, normalizedSkip);
        return Ok(shows);
    }

    /// <summary>
    /// Retrieves all-time greatest shows for the "All-Time Greats" carousel.
    /// No authentication required. Implements lazy cache refresh.
    /// </summary>
    /// <param name="take">Maximum number of shows to return (defaults to 20)</param>
    /// <param name="skip">Number of shows to skip for pagination (defaults to 0)</param>
    /// <returns>List of all-time greatest shows</returns>
    [HttpGet("all-time-greats")]
    public async Task<ActionResult<IReadOnlyList<ShowSearchItemDto>>> GetAllTimeGreats([FromQuery] int take = 20, [FromQuery] int skip = 0)
    {
        var (normalizedTake, normalizedSkip) = NormalizePaging(take, skip);

        var shows = await _discoverService.GetAllTimeGreatsAsync(normalizedTake, normalizedSkip);
        return Ok(shows);
    }

    private static (int Take, int Skip) NormalizePaging(int take, int skip)
    {
        // Keep API forgiving for clients: out-of-range values are clamped/fallbacked.
        var normalizedTake = take is >= 1 and <= MaxTake ? take : DefaultTake;
        var normalizedSkip = Math.Max(0, skip);
        return (normalizedTake, normalizedSkip);
    }
}
