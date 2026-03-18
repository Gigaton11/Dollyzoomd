namespace DollyZoomd.Models;

/// <summary>
/// Represents a cached show in a discover category carousel (Popular, All-Time Greats, etc.).
/// Keyed by (CategoryName, RankPosition) for ordering within categories.
/// </summary>
public class DiscoverCache
{
    public int Id { get; set; }
    
    /// <summary>
    /// Category name: "popular", "all-time-greats", etc.
    /// </summary>
    public required string CategoryName { get; set; }
    
    /// <summary>
    /// Display order within the category (0-indexed).
    /// </summary>
    public int RankPosition { get; set; }
    
    /// <summary>
    /// Foreign key to Show entity (TVMaze ID).
    /// </summary>
    public int ShowId { get; set; }
    
    /// <summary>
    /// Navigation property to Show.
    /// </summary>
    public virtual Show? Show { get; set; }
    
    /// <summary>
    /// When this cache entry was last updated.
    /// </summary>
    public DateTime CachedAtUtc { get; set; }
    
    /// <summary>
    /// When this cache entry expires and needs refresh.
    /// </summary>
    public DateTime ExpiryAtUtc { get; set; }
}
