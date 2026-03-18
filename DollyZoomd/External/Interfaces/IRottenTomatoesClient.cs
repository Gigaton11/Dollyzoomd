using DollyZoomd.External.Models;

namespace DollyZoomd.External.Interfaces;

public interface IRottenTomatoesClient
{
    Task<IReadOnlyList<RottenTomatoesPopularEntry>> GetPopularShowEntriesAsync(CancellationToken cancellationToken = default);
}