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
        // Thin controller: query parsing is minimal, filtering/validation is in service layer.
        var results = await showService.SearchShowsAsync(q, cancellationToken);
        return Ok(results);
    }

    [HttpGet("{id:int}/details")]
    [ProducesResponseType(typeof(ShowDetailsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ShowDetailsDto>> GetDetails([FromRoute] int id, CancellationToken cancellationToken)
    {
        // Details endpoint aggregates show, cast, and episodes into one payload.
        var details = await showService.GetShowDetailsAsync(id, cancellationToken);
        return Ok(details);
    }
}
