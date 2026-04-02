using DollyZoomd.DTOs.Shows;
using DollyZoomd.External.Interfaces;
using DollyZoomd.Services.Interfaces;

namespace DollyZoomd.Services;

public class ShowService(ITvMazeClient tvMazeClient) : IShowService
{
    public async Task<IReadOnlyList<ShowSearchItemDto>> SearchShowsAsync(string query, CancellationToken cancellationToken = default)
    {
        // Normalize and validate early so downstream queries are predictable.
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

        // The search pipeline keeps only valid rows, maps external models to API DTOs,
        // removes duplicates, and caps result size for responsive UI consumption.
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

    public async Task<ShowDetailsDto> GetShowDetailsAsync(int showId, CancellationToken cancellationToken = default)
    {
        if (showId <= 0)
        {
            throw new ArgumentException("Show ID must be a positive integer.");
        }

        // Fetch independent resources in parallel to reduce total latency.
        var showTask = tvMazeClient.GetShowByIdAsync(showId, cancellationToken);
        var episodesTask = tvMazeClient.GetShowEpisodesAsync(showId, cancellationToken);
        var castTask = tvMazeClient.GetShowCastAsync(showId, cancellationToken);

        await Task.WhenAll(showTask, episodesTask, castTask);

        var show = showTask.Result;
        if (show is null)
        {
            throw new KeyNotFoundException("Show not found.");
        }

        // Keep episode ordering deterministic for clients by season, then number.
        var episodes = episodesTask.Result
            .Where(episode => episode.Id > 0 && !string.IsNullOrWhiteSpace(episode.Name))
            .Select(episode => new ShowDetailsEpisodeDto
            {
                EpisodeId = episode.Id,
                Name = episode.Name,
                Season = Math.Max(episode.Season, 0),
                Number = Math.Max(episode.Number, 0),
                AirDate = TryParseDateOnly(episode.Airdate),
                SummaryHtml = episode.Summary,
                ThumbnailUrl = episode.Image?.Medium ?? episode.Image?.Original
            })
            .OrderBy(episode => episode.Season)
            .ThenBy(episode => episode.Number)
            .ToList();

        // Deduplicate cast by stable person key (ID when available, otherwise normalized name).
        var cast = castTask.Result
            .Where(member => member.Person is not null && !string.IsNullOrWhiteSpace(member.Person.Name))
            .Select(member => new ShowDetailsCastMemberDto
            {
                PersonId = member.Person!.Id,
                PersonName = member.Person.Name,
                CharacterName = string.IsNullOrWhiteSpace(member.Character?.Name) ? "Unknown" : member.Character!.Name,
                PersonImageUrl = member.Person.Image?.Medium
                    ?? member.Person.Image?.Original
                    ?? member.Character?.Image?.Medium
                    ?? member.Character?.Image?.Original
            })
            .DistinctBy(member => member.PersonId > 0
                ? $"id:{member.PersonId}"
                : $"name:{member.PersonName.ToLowerInvariant()}")
            .ToList();

        return new ShowDetailsDto
        {
            TvMazeId = show.Id,
            Name = show.Name,
            PosterUrl = show.Image?.Original ?? show.Image?.Medium,
            BannerUrl = show.Image?.Original ?? show.Image?.Medium,
            SummaryHtml = show.Summary,
            Genres = show.Genres?.Where(genre => !string.IsNullOrWhiteSpace(genre)).ToList() ?? [],
            AverageRating = show.Rating?.Average,
            NetworkName = show.Network?.Name ?? show.WebChannel?.Name,
            Status = show.Status,
            PremieredOn = TryParseDateOnly(show.Premiered),
            EndedOn = TryParseDateOnly(show.Ended),
            Episodes = episodes,
            Cast = cast
        };
    }

    private static DateOnly? TryParseDateOnly(string? value)
    {
        return DateOnly.TryParse(value, out var parsedDate) ? parsedDate : null;
    }
}
