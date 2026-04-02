using DollyZoomd.Data;
using DollyZoomd.Models;
using DollyZoomd.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace DollyZoomd.Repositories;

public class WatchlistRepository(AppDbContext dbContext) : IWatchlistRepository
{
    public Task<WatchlistEntry?> GetEntryAsync(Guid userId, int showId, CancellationToken cancellationToken = default)
    {
        return dbContext.WatchlistEntries
            .Include(x => x.Show)
            .SingleOrDefaultAsync(x => x.UserId == userId && x.ShowId == showId, cancellationToken);
    }

    public async Task<IReadOnlyList<WatchlistEntry>> GetUserWatchlistAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await dbContext.WatchlistEntries
            .Where(x => x.UserId == userId)
            .Include(x => x.Show)
            .OrderByDescending(x => x.UpdatedAtUtc)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(WatchlistEntry entry, CancellationToken cancellationToken = default)
    {
        await dbContext.WatchlistEntries.AddAsync(entry, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(WatchlistEntry entry, CancellationToken cancellationToken = default)
    {
        dbContext.WatchlistEntries.Update(entry);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(WatchlistEntry entry, CancellationToken cancellationToken = default)
    {
        dbContext.WatchlistEntries.Remove(entry);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpsertShowCacheAsync(Show show, CancellationToken cancellationToken = default)
    {
        var existing = await dbContext.Shows.FindAsync([show.Id], cancellationToken);
        if (existing is null)
        {
            await dbContext.Shows.AddAsync(show, cancellationToken);
        }
        else
        {
            CopyShowCacheFields(show, existing);
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static void CopyShowCacheFields(Show source, Show target)
    {
        target.Name = source.Name;
        target.PosterUrl = source.PosterUrl;
        target.GenresCsv = source.GenresCsv;
        target.CachedAtUtc = DateTime.UtcNow;
    }
}
