using Microsoft.Extensions.Options;
using DollyZoomd.DTOs.Shows;
using DollyZoomd.External.Interfaces;
using DollyZoomd.External.Models;
using DollyZoomd.Options;
using DollyZoomd.Repositories.Interfaces;
using DollyZoomd.Services.Interfaces;

namespace DollyZoomd.Services;

public class DiscoverService(
    IDiscoverRepository discoverRepository,
    ITvMazeClient tvMazeClient,
    IOptions<DiscoverOptions> discoverOptions,
    ILogger<DiscoverService> logger) : IDiscoverService
{
    private readonly IDiscoverRepository _discoverRepository = discoverRepository;
    private readonly ITvMazeClient _tvMazeClient = tvMazeClient;
    private readonly DiscoverOptions _options = discoverOptions.Value;
    private readonly ILogger<DiscoverService> _logger = logger;

    private const string PopularCategory = "popular";
    private const string AllTimeGreatsCategory = "all-time-greats";

    public async Task<IReadOnlyList<ShowSearchItemDto>> GetPopularShowsAsync(int take = 20, int skip = 0)
    {
        // Check if cache is expired; if so, refresh it
        var isExpired = await _discoverRepository.IsCategoryExpiredAsync(PopularCategory);
        if (isExpired)
        {
            await RefreshPopularCacheAsync();
        }

        return await _discoverRepository.GetDiscoverShowsAsync(PopularCategory, take, skip);
    }

    public async Task<IReadOnlyList<ShowSearchItemDto>> GetAllTimeGreatsAsync(int take = 20, int skip = 0)
    {
        // Check if cache is expired; if so, refresh it
        var isExpired = await _discoverRepository.IsCategoryExpiredAsync(AllTimeGreatsCategory);

        // If config changed from a short legacy list to a larger curated list, refresh immediately.
        if (!isExpired)
        {
            var configuredTitleCount = _options.AllTimeGreatsTitles.Count(title => !string.IsNullOrWhiteSpace(title));
            if (configuredTitleCount > 0)
            {
                var cachedCount = await _discoverRepository.GetCategoryCountAsync(AllTimeGreatsCategory);
                var minimumExpectedCount = Math.Max(1, configuredTitleCount / 2);
                if (cachedCount < minimumExpectedCount)
                {
                    _logger.LogInformation(
                        "All-time cache has {CachedCount} rows but curated list has {ConfiguredCount} titles. Forcing refresh.",
                        cachedCount,
                        configuredTitleCount);
                    isExpired = true;
                }
            }
        }

        if (isExpired)
        {
            await RefreshAllTimeGreatsCacheAsync();
        }

        return await _discoverRepository.GetDiscoverShowsAsync(AllTimeGreatsCategory, take, skip);
    }

    /// <summary>
    /// Refreshes the popular shows cache by fetching from TVMaze.
    /// In a real implementation, you might query TVMaze's schedule or trending endpoint.
    /// For now, we use a curated list of popular show IDs.
    /// </summary>
    private async Task RefreshPopularCacheAsync()
    {
        try
        {
            // Fetch a representative set of popular shows from TVMaze
            // This is a simplified approach; in production you'd want TVMaze's trending or schedule endpoint
            var popularShowIds = new[] { 1, 82, 121, 235, 530, 1403, 1399, 190, 216, 361, 240, 271 };
            var shows = await _tvMazeClient.GetShowsByIdsAsync(popularShowIds.ToList());

            var dtos = shows
                .Select(x => x.Show)
                .Where(show => show is not null)
                .Select(show => MapToDto(show!))
                .DistinctBy(dto => dto.TvMazeId)
                .ToList();

            await _discoverRepository.RefreshDiscoverCacheAsync(PopularCategory, dtos, _options.PopularCacheTtlHours);
            _logger.LogInformation("Refreshed popular shows cache with {Count} shows.", dtos.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to refresh popular shows cache.");
            throw;
        }
    }

    /// <summary>
    /// Refreshes the all-time-greats shows cache using the fixed curated title list from configuration.
    /// Titles are resolved in order and stored in that same display order.
    /// </summary>
    private async Task RefreshAllTimeGreatsCacheAsync()
    {
        try
        {
            var curatedTitles = _options.AllTimeGreatsTitles
                .Where(title => !string.IsNullOrWhiteSpace(title))
                .Select(title => title.Trim())
                .ToList();

            if (curatedTitles.Count == 0 && _options.AllTimeGreatsTvMazeIds.Length == 0)
            {
                _logger.LogWarning("No all-time-greats titles or fallback TVMaze IDs configured.");
                return;
            }

            var dtos = new List<ShowSearchItemDto>();
            var seenShowIds = new HashSet<int>();

            if (curatedTitles.Count > 0)
            {
                foreach (var title in curatedTitles)
                {
                    var searchResults = await _tvMazeClient.SearchShowsAsync(title);
                    var selectedShow = SelectBestMatch(title, searchResults);
                    if (selectedShow is null)
                    {
                        _logger.LogWarning("All-time curated title '{Title}' could not be resolved from TVMaze.", title);
                        continue;
                    }

                    if (!seenShowIds.Add(selectedShow.Id))
                    {
                        _logger.LogInformation("Skipping duplicate all-time show '{Title}' (ID {TvMazeId}).", selectedShow.Name, selectedShow.Id);
                        continue;
                    }

                    dtos.Add(MapToDto(selectedShow));
                }
            }
            else
            {
                // Backward-compatible fallback if only IDs are configured.
                var shows = await _tvMazeClient.GetShowsByIdsAsync(_options.AllTimeGreatsTvMazeIds.ToList());
                dtos = shows
                    .Select(x => x.Show)
                    .Where(show => show is not null)
                    .Select(show => MapToDto(show!))
                    .DistinctBy(dto => dto.TvMazeId)
                    .ToList();
            }

            if (dtos.Count == 0)
            {
                _logger.LogWarning("All-time-greats refresh produced zero shows; keeping existing cache.");
                return;
            }

            await _discoverRepository.RefreshDiscoverCacheAsync(AllTimeGreatsCategory, dtos, _options.AllTimeGreatsCacheTtlHours);
            _logger.LogInformation("Refreshed all-time-greats cache with {Count} shows.", dtos.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to refresh all-time-greats cache.");
            throw;
        }
    }

    /// <summary>
    /// Safely parses a date string to DateOnly.
    /// Returns null if parsing fails or string is empty.
    /// </summary>
    private static DateOnly? TryParseDateOnly(string? dateString)
    {
        if (string.IsNullOrWhiteSpace(dateString))
        {
            return null;
        }

        return DateOnly.TryParse(dateString, out var date) ? date : null;
    }

    private static ShowSearchItemDto MapToDto(TvMazeShow show)
    {
        return new ShowSearchItemDto
        {
            TvMazeId = show.Id,
            Name = show.Name,
            PosterUrl = show.Image?.Medium ?? show.Image?.Original,
            Genres = show.Genres?.Where(g => !string.IsNullOrWhiteSpace(g)).ToList() ?? [],
            PremieredOn = TryParseDateOnly(show.Premiered),
            AverageRating = show.Rating?.Average
        };
    }

    private static TvMazeShow? SelectBestMatch(string title, IReadOnlyList<TvMazeSearchResult> searchResults)
    {
        var candidates = searchResults
            .Select(result => result.Show)
            .Where(show => show is not null && show.Id > 0)
            .Select(show => show!)
            .ToList();

        if (candidates.Count == 0)
        {
            return null;
        }

        var normalizedTitle = NormalizeTitle(title);

        var exact = candidates.FirstOrDefault(show =>
            string.Equals(NormalizeTitle(show.Name), normalizedTitle, StringComparison.OrdinalIgnoreCase));
        if (exact is not null)
        {
            return exact;
        }

        var startsWith = candidates.FirstOrDefault(show =>
            NormalizeTitle(show.Name).StartsWith(normalizedTitle, StringComparison.OrdinalIgnoreCase));
        if (startsWith is not null)
        {
            return startsWith;
        }

        return candidates
            .OrderByDescending(show => show.Rating?.Average ?? 0)
            .FirstOrDefault();
    }

    private static string NormalizeTitle(string value)
    {
        return new string(value
            .Where(char.IsLetterOrDigit)
            .Select(char.ToLowerInvariant)
            .ToArray());
    }
}
