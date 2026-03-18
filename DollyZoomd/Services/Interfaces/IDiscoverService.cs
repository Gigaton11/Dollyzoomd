using DollyZoomd.DTOs.Shows;

namespace DollyZoomd.Services.Interfaces;

/// <summary>
/// Service for managing discover carousel content.
/// Handles retrieving popular and critically-acclaimed shows with automatic cache refresh.
/// </summary>
public interface IDiscoverService
{
    /// <summary>
    /// Ensures the popular discover cache is fresh.
    /// Used by background refresh automation and request-time stale checks.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    Task EnsurePopularShowsFreshAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves popular shows for the "Popular Right Now" carousel.
    /// Implements lazy refresh: checks cache expiration and refreshes if needed.
    /// </summary>
    /// <param name="take">Number of shows to return (defaults to 20)</param>
    /// <param name="skip">Number of shows to skip for pagination (defaults to 0)</param>
    /// <returns>List of popular shows as ShowSearchItemDto</returns>
    Task<IReadOnlyList<ShowSearchItemDto>> GetPopularShowsAsync(int take = 20, int skip = 0);
    
    /// <summary>
    /// Retrieves all-time greatest shows for the "All-Time Greats" carousel.
    /// Implements lazy refresh: checks cache expiration and refreshes if needed.
    /// </summary>
    /// <param name="take">Number of shows to return (defaults to 20)</param>
    /// <param name="skip">Number of shows to skip for pagination (defaults to 0)</param>
    /// <returns>List of all-time greatest shows as ShowSearchItemDto</returns>
    Task<IReadOnlyList<ShowSearchItemDto>> GetAllTimeGreatsAsync(int take = 20, int skip = 0);
}
