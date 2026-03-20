using DollyZoomd.DTOs.Shows;

namespace DollyZoomd.Services.Interfaces;

public interface IShowService
{
    Task<IReadOnlyList<ShowSearchItemDto>> SearchShowsAsync(string query, CancellationToken cancellationToken = default);
    Task<ShowDetailsDto> GetShowDetailsAsync(int showId, CancellationToken cancellationToken = default);
}
