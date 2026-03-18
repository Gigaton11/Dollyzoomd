using DollyZoomd.DTOs.Profile;
using DollyZoomd.Models;

namespace DollyZoomd.Repositories.Interfaces;

public interface IProfileRepository
{
    Task<User?> GetUserByUsernameAsync(string username, CancellationToken cancellationToken = default);
    Task<WatchlistSummaryDto> GetWatchlistSummaryAsync(Guid userId, CancellationToken cancellationToken = default);
}
