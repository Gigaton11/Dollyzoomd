using System.Net;
using System.Net.Http.Json;
using DollyZoomd.Exceptions;
using DollyZoomd.External.Interfaces;
using DollyZoomd.External.Models;

namespace DollyZoomd.External;

public class TvMazeClient(IHttpClientFactory httpClientFactory, ILogger<TvMazeClient> logger) : ITvMazeClient
{
    public async Task<IReadOnlyList<TvMazeSearchResult>> SearchShowsAsync(string query, CancellationToken cancellationToken = default)
    {
        var client = httpClientFactory.CreateClient("TVMaze");
        var encodedQuery = Uri.EscapeDataString(query);

        try
        {
            using var response = await client.GetAsync($"/search/shows?q={encodedQuery}", cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning("TVMaze search failed with status {StatusCode}.", response.StatusCode);
                throw new ExternalServiceUnavailableException("TVMaze is currently unavailable. Please try again shortly.");
            }

            var results = await response.Content.ReadFromJsonAsync<List<TvMazeSearchResult>>(cancellationToken: cancellationToken);
            return results ?? [];
        }
        catch (TaskCanceledException ex)
        {
            logger.LogWarning(ex, "TVMaze search timed out.");
            throw new ExternalServiceUnavailableException("TVMaze request timed out. Please try again shortly.");
        }
        catch (HttpRequestException ex)
        {
            logger.LogWarning(ex, "TVMaze network error.");
            throw new ExternalServiceUnavailableException("TVMaze is currently unavailable. Please try again shortly.");
        }
    }

    public async Task<IReadOnlyList<TvMazeSearchResult>> GetShowsByIdsAsync(IReadOnlyList<int> showIds, CancellationToken cancellationToken = default)
    {
        var client = httpClientFactory.CreateClient("TVMaze");
        var results = new List<TvMazeSearchResult>();

        foreach (var showId in showIds)
        {
            try
            {
                using var response = await client.GetAsync($"/shows/{showId}", cancellationToken);
                if (response.IsSuccessStatusCode)
                {
                    var show = await response.Content.ReadFromJsonAsync<TvMazeShow>(cancellationToken: cancellationToken);
                    if (show != null)
                    {
                        results.Add(new TvMazeSearchResult { Show = show });
                    }
                }
                else
                {
                    logger.LogWarning("TVMaze fetch for show {ShowId} failed with status {StatusCode}.", showId, response.StatusCode);
                }
            }
            catch (TaskCanceledException ex)
            {
                logger.LogWarning(ex, "TVMaze fetch for show {ShowId} timed out.", showId);
            }
            catch (HttpRequestException ex)
            {
                logger.LogWarning(ex, "TVMaze network error while fetching show {ShowId}.", showId);
            }
        }

        return results.AsReadOnly();
    }

    public async Task<TvMazeShow?> GetShowByIdAsync(int showId, CancellationToken cancellationToken = default)
    {
        if (showId <= 0)
        {
            throw new ArgumentException("Show ID must be a positive integer.");
        }

        var client = httpClientFactory.CreateClient("TVMaze");

        try
        {
            using var response = await client.GetAsync($"/shows/{showId}", cancellationToken);
            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                return null;
            }

            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning("TVMaze fetch for show {ShowId} failed with status {StatusCode}.", showId, response.StatusCode);
                throw new ExternalServiceUnavailableException("TVMaze is currently unavailable. Please try again shortly.");
            }

            return await response.Content.ReadFromJsonAsync<TvMazeShow>(cancellationToken: cancellationToken);
        }
        catch (TaskCanceledException ex)
        {
            logger.LogWarning(ex, "TVMaze fetch for show {ShowId} timed out.", showId);
            throw new ExternalServiceUnavailableException("TVMaze request timed out. Please try again shortly.");
        }
        catch (HttpRequestException ex)
        {
            logger.LogWarning(ex, "TVMaze network error while fetching show {ShowId}.", showId);
            throw new ExternalServiceUnavailableException("TVMaze is currently unavailable. Please try again shortly.");
        }
    }

    public async Task<IReadOnlyList<TvMazeEpisode>> GetShowEpisodesAsync(int showId, CancellationToken cancellationToken = default)
    {
        if (showId <= 0)
        {
            throw new ArgumentException("Show ID must be a positive integer.");
        }

        var client = httpClientFactory.CreateClient("TVMaze");

        try
        {
            using var response = await client.GetAsync($"/shows/{showId}/episodes", cancellationToken);
            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                return [];
            }

            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning("TVMaze episodes fetch for show {ShowId} failed with status {StatusCode}.", showId, response.StatusCode);
                throw new ExternalServiceUnavailableException("TVMaze is currently unavailable. Please try again shortly.");
            }

            var episodes = await response.Content.ReadFromJsonAsync<List<TvMazeEpisode>>(cancellationToken: cancellationToken);
            return episodes ?? [];
        }
        catch (TaskCanceledException ex)
        {
            logger.LogWarning(ex, "TVMaze episodes fetch for show {ShowId} timed out.", showId);
            throw new ExternalServiceUnavailableException("TVMaze request timed out. Please try again shortly.");
        }
        catch (HttpRequestException ex)
        {
            logger.LogWarning(ex, "TVMaze network error while fetching episodes for show {ShowId}.", showId);
            throw new ExternalServiceUnavailableException("TVMaze is currently unavailable. Please try again shortly.");
        }
    }

    public async Task<IReadOnlyList<TvMazeCastMember>> GetShowCastAsync(int showId, CancellationToken cancellationToken = default)
    {
        if (showId <= 0)
        {
            throw new ArgumentException("Show ID must be a positive integer.");
        }

        var client = httpClientFactory.CreateClient("TVMaze");

        try
        {
            using var response = await client.GetAsync($"/shows/{showId}/cast", cancellationToken);
            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                return [];
            }

            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning("TVMaze cast fetch for show {ShowId} failed with status {StatusCode}.", showId, response.StatusCode);
                throw new ExternalServiceUnavailableException("TVMaze is currently unavailable. Please try again shortly.");
            }

            var cast = await response.Content.ReadFromJsonAsync<List<TvMazeCastMember>>(cancellationToken: cancellationToken);
            return cast ?? [];
        }
        catch (TaskCanceledException ex)
        {
            logger.LogWarning(ex, "TVMaze cast fetch for show {ShowId} timed out.", showId);
            throw new ExternalServiceUnavailableException("TVMaze request timed out. Please try again shortly.");
        }
        catch (HttpRequestException ex)
        {
            logger.LogWarning(ex, "TVMaze network error while fetching cast for show {ShowId}.", showId);
            throw new ExternalServiceUnavailableException("TVMaze is currently unavailable. Please try again shortly.");
        }
    }
}
