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
    IRottenTomatoesClient rottenTomatoesClient,
    IOptions<DiscoverOptions> discoverOptions,
    ILogger<DiscoverService> logger) : IDiscoverService
{
    private const int PopularTopCount = 25;
    private static readonly int[] DefaultPopularFallbackTvMazeIds = [1, 82, 121, 235, 530, 1403, 1399, 190, 216, 361, 240, 271];
    private static readonly SemaphoreSlim PopularRefreshLock = new(1, 1);

    private readonly IDiscoverRepository _discoverRepository = discoverRepository;
    private readonly ITvMazeClient _tvMazeClient = tvMazeClient;
    private readonly IRottenTomatoesClient _rottenTomatoesClient = rottenTomatoesClient;
    private readonly DiscoverOptions _options = discoverOptions.Value;
    private readonly ILogger<DiscoverService> _logger = logger;

    private const string PopularCategory = "popular";
    private const string AllTimeGreatsCategory = "all-time-greats";

    public async Task<IReadOnlyList<ShowSearchItemDto>> GetPopularShowsAsync(int take = 20, int skip = 0)
    {
        await EnsurePopularShowsFreshAsync();

        return await _discoverRepository.GetDiscoverShowsAsync(PopularCategory, take, skip);
    }

    public async Task EnsurePopularShowsFreshAsync(CancellationToken cancellationToken = default)
    {
        // Fast path: if cache is valid and sufficiently populated, serve immediately.
        var isExpired = await _discoverRepository.IsCategoryExpiredAsync(PopularCategory);
        var cachedCount = await _discoverRepository.GetCategoryCountAsync(PopularCategory);
        if (!isExpired && cachedCount >= PopularTopCount)
        {
            return;
        }

        // Single-flight lock prevents multiple concurrent refreshes under load.
        await PopularRefreshLock.WaitAsync(cancellationToken);

        try
        {
            isExpired = await _discoverRepository.IsCategoryExpiredAsync(PopularCategory);
            cachedCount = await _discoverRepository.GetCategoryCountAsync(PopularCategory);
            if (!isExpired && cachedCount >= PopularTopCount)
            {
                return;
            }

            try
            {
                await RefreshPopularCacheAsync(cancellationToken);
            }
            catch (Exception ex) when (cachedCount > 0)
            {
                // Degrade gracefully: stale cache is preferable to hard failure for browse pages.
                _logger.LogWarning(ex, "Popular refresh failed. Serving stale cache with {Count} rows.", cachedCount);
            }
        }
        finally
        {
            PopularRefreshLock.Release();
        }
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
    /// Refreshes the popular shows cache using Rotten Tomatoes' ranked list, then resolves each title through TVMaze.
    /// If the source changes or some entries cannot be resolved, the method backfills missing rows from fallback TVMaze IDs.
    /// </summary>
    private async Task RefreshPopularCacheAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var dtos = new List<ShowSearchItemDto>();

            try
            {
                var popularEntries = await _rottenTomatoesClient.GetPopularShowEntriesAsync(cancellationToken);
                dtos = await ResolvePopularShowsFromSourceAsync(popularEntries, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to fetch Rotten Tomatoes popular shows. Falling back to curated TVMaze IDs.");
            }

            if (dtos.Count < PopularTopCount)
            {
                // Backfill keeps carousel size stable when source matching is incomplete.
                var fallbackShows = await ResolveShowsByIdsAsync(DefaultPopularFallbackTvMazeIds, cancellationToken);
                AppendMissingShows(dtos, fallbackShows, PopularTopCount);
            }

            if (dtos.Count == 0)
            {
                _logger.LogWarning("Popular refresh produced zero shows; keeping existing cache.");
                return;
            }

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
                    var selectedShow = SelectBestMatch(title, null, searchResults);
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

    private async Task<List<ShowSearchItemDto>> ResolvePopularShowsFromSourceAsync(
        IReadOnlyList<RottenTomatoesPopularEntry> popularEntries,
        CancellationToken cancellationToken)
    {
        var dtos = new List<ShowSearchItemDto>();
        var seenShowIds = new HashSet<int>();

        foreach (var entry in popularEntries.OrderBy(entry => entry.Rank))
        {
            var searchResults = await _tvMazeClient.SearchShowsAsync(entry.Title, cancellationToken);
            var selectedShow = SelectBestMatch(entry.Title, entry.YearHint, searchResults);
            if (selectedShow is null)
            {
                _logger.LogWarning("Popular title '{Title}' could not be resolved from TVMaze.", entry.Title);
                continue;
            }

            if (!seenShowIds.Add(selectedShow.Id))
            {
                _logger.LogInformation("Skipping duplicate popular show '{Title}' (ID {TvMazeId}).", selectedShow.Name, selectedShow.Id);
                continue;
            }

            dtos.Add(MapToDto(selectedShow));
        }

        return dtos;
    }

    private async Task<List<ShowSearchItemDto>> ResolveShowsByIdsAsync(IReadOnlyList<int> showIds, CancellationToken cancellationToken)
    {
        var shows = await _tvMazeClient.GetShowsByIdsAsync(showIds, cancellationToken);

        return shows
            .Select(result => result.Show)
            .Where(show => show is not null)
            .Select(show => MapToDto(show!))
            .DistinctBy(dto => dto.TvMazeId)
            .ToList();
    }

    private static void AppendMissingShows(List<ShowSearchItemDto> target, IReadOnlyList<ShowSearchItemDto> fallbackShows, int maxCount)
    {
        var existingIds = target.Select(show => show.TvMazeId).ToHashSet();

        foreach (var fallbackShow in fallbackShows)
        {
            if (target.Count >= maxCount)
            {
                break;
            }

            if (!existingIds.Add(fallbackShow.TvMazeId))
            {
                continue;
            }

            target.Add(fallbackShow);
        }
    }

    private static TvMazeShow? SelectBestMatch(string title, int? yearHint, IReadOnlyList<TvMazeSearchResult> searchResults)
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

        // Selection strategy prioritizes precision before recall:
        // 1) exact normalized title + exact year
        // 2) exact normalized title
        // 3) year-only match
        // 4) starts-with title match
        // 5) highest rated fallback
        if (yearHint is not null)
        {
            var exactYearMatch = FindHighestRated(candidates,
                show => IsNormalizedTitleMatch(show, normalizedTitle) && GetPremieredYear(show) == yearHint);

            if (exactYearMatch is not null)
            {
                return exactYearMatch;
            }
        }

        var exact = FindHighestRated(candidates, show => IsNormalizedTitleMatch(show, normalizedTitle));
        if (exact is not null)
        {
            return exact;
        }

        if (yearHint is not null)
        {
            var yearMatch = FindHighestRated(candidates, show => GetPremieredYear(show) == yearHint);

            if (yearMatch is not null)
            {
                return yearMatch;
            }
        }

        var startsWith = FindHighestRated(candidates,
            show => NormalizeTitle(show.Name).StartsWith(normalizedTitle, StringComparison.OrdinalIgnoreCase));
        if (startsWith is not null)
        {
            return startsWith;
        }

        return FindHighestRated(candidates, _ => true);
    }

    private static TvMazeShow? FindHighestRated(IEnumerable<TvMazeShow> candidates, Func<TvMazeShow, bool> predicate)
    {
        return candidates
            .Where(predicate)
            .OrderByDescending(show => show.Rating?.Average ?? 0)
            .FirstOrDefault();
    }

    private static bool IsNormalizedTitleMatch(TvMazeShow show, string normalizedTitle)
    {
        return string.Equals(NormalizeTitle(show.Name), normalizedTitle, StringComparison.OrdinalIgnoreCase);
    }

    private static int? GetPremieredYear(TvMazeShow show)
    {
        if (string.IsNullOrWhiteSpace(show.Premiered))
        {
            return null;
        }

        return DateOnly.TryParse(show.Premiered, out var premieredDate)
            ? premieredDate.Year
            : null;
    }

    private static string NormalizeTitle(string value)
    {
        return new string(value
            .Where(char.IsLetterOrDigit)
            .Select(char.ToLowerInvariant)
            .ToArray());
    }
}
