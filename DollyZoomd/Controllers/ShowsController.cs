using DollyZoomd.DTOs.Shows;
using DollyZoomd.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DollyZoomd.Controllers;

[ApiController]
[Route("api/shows")]
[AllowAnonymous]
public class ShowsController(IShowService showService) : ControllerBase
{
    [HttpGet("search")]
    [ProducesResponseType(typeof(IReadOnlyList<ShowSearchItemDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<IReadOnlyList<ShowSearchItemDto>>> Search([FromQuery] string q, CancellationToken cancellationToken)
    {
        var results = await showService.SearchShowsAsync(q, cancellationToken);
        return Ok(results);
    }
}
