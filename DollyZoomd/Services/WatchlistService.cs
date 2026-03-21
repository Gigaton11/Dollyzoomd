using DollyZoomd.DTOs.Watchlist;
using DollyZoomd.Models;
using DollyZoomd.Repositories.Interfaces;
using DollyZoomd.Services.Interfaces;

namespace DollyZoomd.Services;

public class WatchlistService(IWatchlistRepository watchlistRepository) : IWatchlistService
{
    public async Task AddToWatchlistAsync(Guid userId, AddToWatchlistRequest request, CancellationToken cancellationToken = default)
    {
        var existing = await watchlistRepository.GetEntryAsync(userId, request.TvMazeShowId, cancellationToken);
        if (existing is not null)
        {
            throw new InvalidOperationException("This show is already in your watchlist.");
        }

        // Ensure the show is cached locally before creating the FK-constrained watchlist row.
        var show = new Show
        {
            Id         = request.TvMazeShowId,
            Name       = request.ShowName,
            PosterUrl  = request.PosterUrl,
            GenresCsv  = request.GenresCsv
        };
        await watchlistRepository.UpsertShowCacheAsync(show, cancellationToken);

        var entry = new WatchlistEntry
        {
            UserId       = userId,
            ShowId       = request.TvMazeShowId,
            Status       = request.Status,
            UpdatedAtUtc = DateTime.UtcNow
        };
        await watchlistRepository.AddAsync(entry, cancellationToken);
    }

    public async Task UpdateStatusAsync(Guid userId, int showId, UpdateWatchStatusRequest request, CancellationToken cancellationToken = default)
    {
        var entry = await GetEntryOrThrowAsync(userId, showId, cancellationToken);
        entry.Status = request.Status;
        await watchlistRepository.UpdateAsync(entry, cancellationToken);
    }

    public async Task RateShowAsync(Guid userId, int showId, RateShowRequest request, CancellationToken cancellationToken = default)
    {
        var entry = await GetEntryOrThrowAsync(userId, showId, cancellationToken);
        entry.Rating = request.Rating;
        await watchlistRepository.UpdateAsync(entry, cancellationToken);
    }

    public async Task RemoveFromWatchlistAsync(Guid userId, int showId, CancellationToken cancellationToken = default)
    {
        var entry = await GetEntryOrThrowAsync(userId, showId, cancellationToken);
        await watchlistRepository.DeleteAsync(entry, cancellationToken);
    }

    public async Task<IReadOnlyList<WatchlistEntryDto>> GetWatchlistAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var entries = await watchlistRepository.GetUserWatchlistAsync(userId, cancellationToken);

        return entries.Select(e => new WatchlistEntryDto
        {
            ShowId       = e.ShowId,
            ShowName     = e.Show?.Name ?? string.Empty,
            PosterUrl    = e.Show?.PosterUrl,
            Genres       = ParseGenres(e.Show?.GenresCsv),
            Status       = e.Status,
            Rating       = e.Rating,
            UpdatedAtUtc = e.UpdatedAtUtc
        }).ToList();
    }

    private async Task<WatchlistEntry> GetEntryOrThrowAsync(Guid userId, int showId, CancellationToken cancellationToken)
    {
        var entry = await watchlistRepository.GetEntryAsync(userId, showId, cancellationToken);
        if (entry is null)
        {
            throw new KeyNotFoundException("This show is not in your watchlist.");
        }
        return entry;
    }

    private static IReadOnlyList<string> ParseGenres(string? csv)
    {
        if (string.IsNullOrWhiteSpace(csv)) return [];
        return csv.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    }
}
