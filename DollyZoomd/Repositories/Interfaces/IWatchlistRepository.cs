using DollyZoomd.Models;

namespace DollyZoomd.Repositories.Interfaces;

public interface IWatchlistRepository
{
    Task<WatchlistEntry?> GetEntryAsync(Guid userId, int showId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<WatchlistEntry>> GetUserWatchlistAsync(Guid userId, CancellationToken cancellationToken = default);
    Task AddAsync(WatchlistEntry entry, CancellationToken cancellationToken = default);
    Task UpdateAsync(WatchlistEntry entry, CancellationToken cancellationToken = default);
    Task DeleteAsync(WatchlistEntry entry, CancellationToken cancellationToken = default);
    Task UpsertShowCacheAsync(Show show, CancellationToken cancellationToken = default);
}
