using DollyZoomd.Data;
using DollyZoomd.Models;
using DollyZoomd.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace DollyZoomd.Repositories;

public class AuthRepository(AppDbContext dbContext) : IAuthRepository
{
    public Task<bool> UsernameExistsAsync(string username, CancellationToken cancellationToken = default)
    {
        var normalizedUsername = (username ?? string.Empty).Trim().ToUpperInvariant();
        return dbContext.Users.AnyAsync(x => x.Username.ToUpper() == normalizedUsername, cancellationToken);
    }

    public Task<bool> EmailExistsAsync(string email, CancellationToken cancellationToken = default)
    {
        var normalizedEmail = (email ?? string.Empty).Trim().ToLowerInvariant();
        return dbContext.Users.AnyAsync(x => x.Email.ToLower() == normalizedEmail, cancellationToken);
    }

    public Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        var normalizedEmail = (email ?? string.Empty).Trim().ToLowerInvariant();
        return dbContext.Users.SingleOrDefaultAsync(x => x.Email.ToLower() == normalizedEmail, cancellationToken);
    }

    public Task<User?> GetByUsernameAsync(string username, CancellationToken cancellationToken = default)
    {
        var normalizedUsername = (username ?? string.Empty).Trim().ToUpperInvariant();
        return dbContext.Users.SingleOrDefaultAsync(x => x.Username.ToUpper() == normalizedUsername, cancellationToken);
    }

    public async Task AddAsync(User user, CancellationToken cancellationToken = default)
    {
        await dbContext.Users.AddAsync(user, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
