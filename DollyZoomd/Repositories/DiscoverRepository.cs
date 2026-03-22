using Microsoft.EntityFrameworkCore;
using DollyZoomd.Data;
using DollyZoomd.DTOs.Shows;
using DollyZoomd.Models;
using DollyZoomd.Repositories.Interfaces;

namespace DollyZoomd.Repositories;

public class DiscoverRepository(AppDbContext context) : IDiscoverRepository
{
    public async Task<IReadOnlyList<ShowSearchItemDto>> GetDiscoverShowsAsync(string categoryName, int take = 20, int skip = 0)
    {
        var cached = await context.DiscoverCaches
            .Where(dc => dc.CategoryName == categoryName)
            .OrderBy(dc => dc.RankPosition)
            .Skip(skip)
            .Take(take)
            .Include(dc => dc.Show)
            .ToListAsync();

        return cached
            .Select(dc => new ShowSearchItemDto
            {
                TvMazeId = dc.Show!.Id,
                Name = dc.Show.Name,
                PosterUrl = dc.Show.PosterUrl,
                Genres = dc.Show.GenresCsv?.Split(',')
                    .Select(g => g.Trim())
                    .Where(g => !string.IsNullOrWhiteSpace(g))
                    .ToList() ?? [],
                PremieredOn = dc.Show.PremieredOn,
                AverageRating = dc.Show.AverageRating
            })
            .ToList()
            .AsReadOnly();
    }

    public async Task RefreshDiscoverCacheAsync(string categoryName, IReadOnlyList<ShowSearchItemDto> shows, int cacheTtlHours)
    {
        // First, ensure all shows exist in the Show cache
        foreach (var show in shows)
        {
            await UpsertShowCacheAsync(show);
        }

        // Delete old cache entries for this category
        var oldEntries = await context.DiscoverCaches
            .Where(dc => dc.CategoryName == categoryName)
            .ToListAsync();

        if (oldEntries.Any())
        {
            context.DiscoverCaches.RemoveRange(oldEntries);
        }

        // Insert new cache entries with calculated expiry
        var now = DateTime.UtcNow;
        var expiry = now.AddHours(cacheTtlHours);
        
        var newEntries = shows
            .Select((show, index) => new DiscoverCache
            {
                CategoryName = categoryName,
                RankPosition = index,
                ShowId = show.TvMazeId,
                CachedAtUtc = now,
                ExpiryAtUtc = expiry
            })
            .ToList();

        await context.DiscoverCaches.AddRangeAsync(newEntries);
        await context.SaveChangesAsync();
    }

    public async Task<bool> IsCategoryExpiredAsync(string categoryName)
    {
        var oldestEntry = await context.DiscoverCaches
            .Where(dc => dc.CategoryName == categoryName)
            .OrderBy(dc => dc.ExpiryAtUtc)
            .FirstOrDefaultAsync();

        if (oldestEntry == null)
        {
            // Category has no entries; consider it expired so it will be refreshed
            return true;
        }

        return DateTime.UtcNow >= oldestEntry.ExpiryAtUtc;
    }

    public Task<int> GetCategoryCountAsync(string categoryName)
    {
        return context.DiscoverCaches
            .Where(dc => dc.CategoryName == categoryName)
            .CountAsync();
    }

    /// <summary>
    /// Inserts or updates show metadata in the Show cache.
    /// Called before creating FK-constrained associations to ensure referential integrity.
    /// </summary>
    private async Task UpsertShowCacheAsync(ShowSearchItemDto show)
    {
        var existing = await context.Shows.FindAsync(show.TvMazeId);

        if (existing == null)
        {
            var newShow = new Show
            {
                Id = show.TvMazeId,
                Name = show.Name,
                PosterUrl = show.PosterUrl ?? string.Empty,
                GenresCsv = string.Join(",", show.Genres),
                PremieredOn = show.PremieredOn,
                AverageRating = show.AverageRating,
                CachedAtUtc = DateTime.UtcNow
            };

            await context.Shows.AddAsync(newShow);
        }
        else
        {
            existing.Name = show.Name;
            existing.PosterUrl = show.PosterUrl ?? string.Empty;
            existing.GenresCsv = string.Join(",", show.Genres);
            existing.PremieredOn = show.PremieredOn;
            existing.AverageRating = show.AverageRating;
            existing.CachedAtUtc = DateTime.UtcNow;
            context.Shows.Update(existing);
        }

        await context.SaveChangesAsync();
    }
}
