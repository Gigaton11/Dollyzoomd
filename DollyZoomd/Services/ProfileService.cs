using DollyZoomd.DTOs.Favorites;
using DollyZoomd.DTOs.Profile;
using DollyZoomd.Repositories.Interfaces;
using DollyZoomd.Services.Interfaces;

namespace DollyZoomd.Services;

public class ProfileService(
    IProfileRepository profileRepository,
    IFavoritesRepository favoritesRepository) : IProfileService
{
    public async Task<UserProfileDto> GetProfileAsync(string username, CancellationToken cancellationToken = default)
    {
        var normalizedUsername = (username ?? string.Empty).Trim();
        if (normalizedUsername.StartsWith('@'))
        {
            normalizedUsername = normalizedUsername[1..];
        }

        if (string.IsNullOrWhiteSpace(normalizedUsername))
        {
            throw new ArgumentException("Username is required.", nameof(username));
        }

        var user = await profileRepository.GetUserByUsernameAsync(normalizedUsername, cancellationToken)
            ?? throw new KeyNotFoundException($"User '{normalizedUsername}' was not found.");

        var summary = await profileRepository.GetWatchlistSummaryAsync(user.Id, cancellationToken);
        var rawFavorites = await favoritesRepository.GetFavoritesAsync(user.Id, cancellationToken);

        var favorites = rawFavorites.Select(f => new FavoriteDto
        {
            ShowId       = f.ShowId,
            ShowName     = f.Show?.Name ?? string.Empty,
            PosterUrl    = f.Show?.PosterUrl,
            Genres       = ParseGenres(f.Show?.GenresCsv),
            DisplayOrder = f.DisplayOrder
        }).ToList();

        return new UserProfileDto
        {
            Username        = user.Username,
            MemberSinceUtc  = user.CreatedAtUtc,
            WatchlistSummary = summary,
            Favorites       = favorites
        };
    }

    private static IReadOnlyList<string> ParseGenres(string? csv)
    {
        if (string.IsNullOrWhiteSpace(csv)) return [];
        return csv.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    }
}
