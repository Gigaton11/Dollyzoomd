using DollyZoomd.DTOs.Profile;
using DollyZoomd.Models;

namespace DollyZoomd.Repositories.Interfaces;

public interface IProfileRepository
{
    Task<User?> GetUserByUsernameAsync(string username, CancellationToken cancellationToken = default);
    Task<User?> GetUserByIdAsync(Guid userId, CancellationToken cancellationToken = default);
    Task UpdateAvatarFileNameAsync(Guid userId, string? avatarFileName, CancellationToken cancellationToken = default);
    Task<WatchlistSummaryDto> GetWatchlistSummaryAsync(Guid userId, CancellationToken cancellationToken = default);
}
