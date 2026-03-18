using DollyZoomd.DTOs.Profile;

namespace DollyZoomd.Services.Interfaces;

public interface IProfileService
{
    Task<UserProfileDto> GetProfileAsync(string username, CancellationToken cancellationToken = default);
}
