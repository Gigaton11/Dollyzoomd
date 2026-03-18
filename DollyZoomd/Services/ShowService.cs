using DollyZoomd.DTOs.Shows;
using DollyZoomd.External.Interfaces;
using DollyZoomd.Services.Interfaces;

namespace DollyZoomd.Services;

public class ShowService(ITvMazeClient tvMazeClient) : IShowService
{
    public async Task<IReadOnlyList<ShowSearchItemDto>> SearchShowsAsync(string query, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            throw new ArgumentException("Search query must be at least 2 characters.");
        }

        var normalizedQuery = query.Trim();
        if (normalizedQuery.Length < 2)
        {
            throw new ArgumentException("Search query must be at least 2 characters.");
        }

        var results = await tvMazeClient.SearchShowsAsync(normalizedQuery, cancellationToken);

        var mappedResults = results
            .Where(x => x.Show is not null)
            .Select(x => x.Show!)
            .Where(x => x.Id > 0 && !string.IsNullOrWhiteSpace(x.Name))
            .Select(x => new ShowSearchItemDto
            {
                TvMazeId = x.Id,
                Name = x.Name,
                PosterUrl = x.Image?.Medium ?? x.Image?.Original,
                Genres = x.Genres?.Where(g => !string.IsNullOrWhiteSpace(g)).ToList() ?? [],
                PremieredOn = TryParseDateOnly(x.Premiered),
                AverageRating = x.Rating?.Average
            })
            .DistinctBy(x => x.TvMazeId)
            .Take(30)
            .ToList();

        return mappedResults;
    }

    private static DateOnly? TryParseDateOnly(string? value)
    {
        return DateOnly.TryParse(value, out var parsedDate) ? parsedDate : null;
    }
}
