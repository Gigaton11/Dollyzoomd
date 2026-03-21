using DollyZoomd.DTOs.Profile;

namespace DollyZoomd.Services.Interfaces;

public interface IProfileService
{
    Task<UserProfileDto> GetProfileAsync(string username, CancellationToken cancellationToken = default);
    Task<string> UpdateAvatarAsync(Guid userId, IFormFile file, CancellationToken cancellationToken = default);
}
