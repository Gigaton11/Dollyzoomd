using DollyZoomd.DTOs.Favorites;

namespace DollyZoomd.Services.Interfaces;

public interface IFavoritesService
{
    Task<IReadOnlyList<FavoriteDto>> GetFavoritesAsync(Guid userId, CancellationToken cancellationToken = default);
    Task AddFavoriteAsync(Guid userId, AddFavoriteRequest request, CancellationToken cancellationToken = default);
    Task RemoveFavoriteAsync(Guid userId, int showId, CancellationToken cancellationToken = default);
}
