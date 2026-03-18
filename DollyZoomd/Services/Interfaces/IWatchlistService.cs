using DollyZoomd.DTOs.Watchlist;

namespace DollyZoomd.Services.Interfaces;

public interface IWatchlistService
{
    Task AddToWatchlistAsync(Guid userId, AddToWatchlistRequest request, CancellationToken cancellationToken = default);
    Task UpdateStatusAsync(Guid userId, int showId, UpdateWatchStatusRequest request, CancellationToken cancellationToken = default);
    Task RateShowAsync(Guid userId, int showId, RateShowRequest request, CancellationToken cancellationToken = default);
    Task RemoveFromWatchlistAsync(Guid userId, int showId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<WatchlistEntryDto>> GetWatchlistAsync(Guid userId, CancellationToken cancellationToken = default);
}
