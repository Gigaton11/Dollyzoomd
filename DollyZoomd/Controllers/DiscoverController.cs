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
        if (take < 1 || take > 100)
            take = 20;
        if (skip < 0)
            skip = 0;

        var shows = await _discoverService.GetPopularShowsAsync(take, skip);
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
        if (take < 1 || take > 100)
            take = 20;
        if (skip < 0)
            skip = 0;

        var shows = await _discoverService.GetAllTimeGreatsAsync(take, skip);
        return Ok(shows);
    }
}
