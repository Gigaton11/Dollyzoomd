using System.Security.Claims;
using DollyZoomd.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DollyZoomd.Controllers;

[ApiController]
[Route("api/profile")]
public class ProfileController(IProfileService profileService) : ControllerBase
{
    [HttpGet("{username}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetProfile(string username, CancellationToken cancellationToken)
    {
        var profile = await profileService.GetProfileAsync(username, cancellationToken);
        return Ok(profile);
    }

    [HttpPut("avatar")]
    [Authorize]
    public async Task<IActionResult> UpdateAvatar([FromForm] IFormFile file, CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        var avatarUrl = await profileService.UpdateAvatarAsync(userId, file, cancellationToken);
        return Ok(new { avatarUrl });
    }

    private Guid GetUserId()
    {
        var claim = User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? throw new UnauthorizedAccessException("User identity not found in token.");
        return Guid.Parse(claim);
    }
}
