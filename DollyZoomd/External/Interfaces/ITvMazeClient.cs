using DollyZoomd.External.Models;

namespace DollyZoomd.External.Interfaces;

public interface ITvMazeClient
{
    Task<IReadOnlyList<TvMazeSearchResult>> SearchShowsAsync(string query, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Fetches show details for multiple TVMaze IDs in order.
    /// Used for discover features where shows are manually curated by TVMaze ID.
    /// </summary>
    /// <param name="showIds">List of TVMaze show IDs to fetch, in desired order</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of TvMazeSearchResult in the same order as showIds, excluding any that could not be fetched</returns>
    Task<IReadOnlyList<TvMazeSearchResult>> GetShowsByIdsAsync(IReadOnlyList<int> showIds, CancellationToken cancellationToken = default);
}
