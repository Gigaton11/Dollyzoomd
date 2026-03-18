using DollyZoomd.Data;
using DollyZoomd.Models;
using DollyZoomd.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace DollyZoomd.Repositories;

public class FavoritesRepository(AppDbContext dbContext) : IFavoritesRepository
{
    public async Task<IReadOnlyList<UserFavorite>> GetFavoritesAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await dbContext.UserFavorites
            .Where(x => x.UserId == userId)
            .Include(x => x.Show)
            .OrderBy(x => x.DisplayOrder)
            .ToListAsync(cancellationToken);
    }

    public Task<UserFavorite?> GetFavoriteAsync(Guid userId, int showId, CancellationToken cancellationToken = default)
    {
        return dbContext.UserFavorites
            .SingleOrDefaultAsync(x => x.UserId == userId && x.ShowId == showId, cancellationToken);
    }

    public Task<int> GetFavoritesCountAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return dbContext.UserFavorites.CountAsync(x => x.UserId == userId, cancellationToken);
    }

    public async Task AddAsync(UserFavorite favorite, CancellationToken cancellationToken = default)
    {
        await dbContext.UserFavorites.AddAsync(favorite, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(UserFavorite favorite, CancellationToken cancellationToken = default)
    {
        dbContext.UserFavorites.Remove(favorite);
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
            existing.Name = show.Name;
            existing.PosterUrl = show.PosterUrl;
            existing.GenresCsv = show.GenresCsv;
            existing.CachedAtUtc = DateTime.UtcNow;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
