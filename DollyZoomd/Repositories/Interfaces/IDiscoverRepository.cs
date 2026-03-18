using DollyZoomd.DTOs.Shows;

namespace DollyZoomd.Repositories.Interfaces;

/// <summary>
/// Repository for managing discover carousel cache data.
/// Handles retrieving cached shows by category and refreshing expired cache entries.
/// </summary>
public interface IDiscoverRepository
{
    /// <summary>
    /// Retrieves discover shows for a given category, ordered by rank position.
    /// </summary>
    /// <param name="categoryName">The discover category (e.g., "popular", "all-time-greats")</param>
    /// <param name="take">Maximum number of results to return (defaults to 20)</param>
    /// <param name="skip">Number of results to skip for pagination (defaults to 0)</param>
    /// <returns>List of ShowSearchItemDto objects</returns>
    Task<IReadOnlyList<ShowSearchItemDto>> GetDiscoverShowsAsync(string categoryName, int take = 20, int skip = 0);
    
    /// <summary>
    /// Refreshes the discover cache for a category with new show data.
    /// Clears old entries and inserts new ones with updated expiry time.
    /// </summary>
    /// <param name="categoryName">The discover category name</param>
    /// <param name="shows">List of shows to cache, in ranked order</param>
    /// <param name="cacheTtlHours">Cache time-to-live in hours (used to calculate ExpiryAtUtc)</param>
    Task RefreshDiscoverCacheAsync(string categoryName, IReadOnlyList<ShowSearchItemDto> shows, int cacheTtlHours);
    
    /// <summary>
    /// Checks if a discover category cache is expired.
    /// </summary>
    /// <param name="categoryName">The discover category name</param>
    /// <returns>True if the oldest entry in the category is past ExpiryAtUtc; false otherwise</returns>
    Task<bool> IsCategoryExpiredAsync(string categoryName);

    /// <summary>
    /// Gets the total number of cached shows currently stored for a category.
    /// </summary>
    /// <param name="categoryName">The discover category name</param>
    /// <returns>Number of cached rows for the category</returns>
    Task<int> GetCategoryCountAsync(string categoryName);
}
