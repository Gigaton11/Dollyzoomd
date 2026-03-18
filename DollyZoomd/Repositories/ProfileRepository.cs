using DollyZoomd.Data;
using DollyZoomd.DTOs.Profile;
using DollyZoomd.Models;
using DollyZoomd.Models.Enums;
using DollyZoomd.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace DollyZoomd.Repositories;

public class ProfileRepository(AppDbContext dbContext) : IProfileRepository
{
    public Task<User?> GetUserByUsernameAsync(string username, CancellationToken cancellationToken = default)
    {
        var normalizedUsername = (username ?? string.Empty).Trim();

        return dbContext.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => EF.Functions.ILike(u.Username, normalizedUsername), cancellationToken);
    }

    public async Task<WatchlistSummaryDto> GetWatchlistSummaryAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var statuses = await dbContext.WatchlistEntries
            .Where(e => e.UserId == userId)
            .Select(e => e.Status)
            .ToListAsync(cancellationToken);

        return new WatchlistSummaryDto
        {
            Total       = statuses.Count,
            Watching    = statuses.Count(s => s == WatchStatus.Watching),
            Completed   = statuses.Count(s => s == WatchStatus.Completed),
            PlanToWatch = statuses.Count(s => s == WatchStatus.PlanToWatch),
            Dropped     = statuses.Count(s => s == WatchStatus.Dropped)
        };
    }
}
