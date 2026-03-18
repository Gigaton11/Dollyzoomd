using DollyZoomd.DTOs.Favorites;
using DollyZoomd.Models;
using DollyZoomd.Repositories.Interfaces;
using DollyZoomd.Services.Interfaces;

namespace DollyZoomd.Services;

public class FavoritesService(IFavoritesRepository favoritesRepository) : IFavoritesService
{
    private const int MaxFavorites = 6;

    public async Task<IReadOnlyList<FavoriteDto>> GetFavoritesAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var favorites = await favoritesRepository.GetFavoritesAsync(userId, cancellationToken);

        return favorites.Select(f => new FavoriteDto
        {
            ShowId       = f.ShowId,
            ShowName     = f.Show?.Name ?? string.Empty,
            PosterUrl    = f.Show?.PosterUrl,
            Genres       = ParseGenres(f.Show?.GenresCsv),
            DisplayOrder = f.DisplayOrder
        }).ToList();
    }

    public async Task AddFavoriteAsync(Guid userId, AddFavoriteRequest request, CancellationToken cancellationToken = default)
    {
        var existing = await favoritesRepository.GetFavoriteAsync(userId, request.TvMazeShowId, cancellationToken);
        if (existing is not null)
        {
            throw new InvalidOperationException("This show is already in your favorites.");
        }

        var existingFavorites = await favoritesRepository.GetFavoritesAsync(userId, cancellationToken);
        var count = existingFavorites.Count;
        if (count >= MaxFavorites)
        {
            throw new InvalidOperationException($"You can only have up to {MaxFavorites} favorites. Remove one to add another.");
        }

        var nextDisplayOrder = existingFavorites
            .Select(f => f.DisplayOrder)
            .DefaultIfEmpty(0)
            .Max() + 1;

        var show = new Show
        {
            Id        = request.TvMazeShowId,
            Name      = request.ShowName,
            PosterUrl = request.PosterUrl,
            GenresCsv = request.GenresCsv
        };
        await favoritesRepository.UpsertShowCacheAsync(show, cancellationToken);

        var favorite = new UserFavorite
        {
            UserId       = userId,
            ShowId       = request.TvMazeShowId,
            DisplayOrder = nextDisplayOrder,
            CreatedAtUtc = DateTime.UtcNow
        };
        await favoritesRepository.AddAsync(favorite, cancellationToken);
    }

    public async Task RemoveFavoriteAsync(Guid userId, int showId, CancellationToken cancellationToken = default)
    {
        var favorite = await favoritesRepository.GetFavoriteAsync(userId, showId, cancellationToken);
        if (favorite is null)
        {
            throw new KeyNotFoundException("This show is not in your favorites.");
        }

        await favoritesRepository.DeleteAsync(favorite, cancellationToken);
    }

    private static IReadOnlyList<string> ParseGenres(string? csv)
    {
        if (string.IsNullOrWhiteSpace(csv)) return [];
        return csv.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    }
}
