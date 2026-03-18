using DollyZoomd.Models;

namespace DollyZoomd.Repositories.Interfaces;

public interface IFavoritesRepository
{
    Task<IReadOnlyList<UserFavorite>> GetFavoritesAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<UserFavorite?> GetFavoriteAsync(Guid userId, int showId, CancellationToken cancellationToken = default);
    Task<int> GetFavoritesCountAsync(Guid userId, CancellationToken cancellationToken = default);
    Task AddAsync(UserFavorite favorite, CancellationToken cancellationToken = default);
    Task DeleteAsync(UserFavorite favorite, CancellationToken cancellationToken = default);
    Task UpsertShowCacheAsync(Show show, CancellationToken cancellationToken = default);
}
