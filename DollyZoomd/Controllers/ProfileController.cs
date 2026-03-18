using DollyZoomd.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DollyZoomd.Controllers;

[ApiController]
[Route("api/profile")]
[AllowAnonymous]
public class ProfileController(IProfileService profileService) : ControllerBase
{
    [HttpGet("{username}")]
    public async Task<IActionResult> GetProfile(string username, CancellationToken cancellationToken)
    {
        var profile = await profileService.GetProfileAsync(username, cancellationToken);
        return Ok(profile);
    }
}
