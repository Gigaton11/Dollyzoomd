using System.Net;
using System.Text.RegularExpressions;
using DollyZoomd.Exceptions;
using DollyZoomd.External.Interfaces;
using DollyZoomd.External.Models;
using DollyZoomd.Options;
using Microsoft.Extensions.Options;

namespace DollyZoomd.External;

public class RottenTomatoesClient(
    IHttpClientFactory httpClientFactory,
    IOptions<DiscoverOptions> discoverOptions,
    ILogger<RottenTomatoesClient> logger) : IRottenTomatoesClient
{
    private static readonly Regex CountdownEntryRegex = new(
        "<div\\s+id=['\\\"]countdown-index-(?<rank>\\d+)['\\\"][^>]*>.*?<a\\s+class=['\\\"]meta-title['\\\"][^>]*href=['\\\"](?<href>[^'\\\"]+)['\\\"][^>]*>(?<title>.*?)</a>",
        RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.Compiled);

    private static readonly Regex HtmlTagRegex = new(@"<.*?>", RegexOptions.Singleline | RegexOptions.Compiled);
    private static readonly Regex SeasonSuffixRegex = new(@":\s*Season\s+\d+\s*$", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex YearHintRegex = new(@"_(?<year>(19|20)\d{2})(?:/|$)", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private readonly DiscoverOptions _discoverOptions = discoverOptions.Value;

    public async Task<IReadOnlyList<RottenTomatoesPopularEntry>> GetPopularShowEntriesAsync(CancellationToken cancellationToken = default)
    {
        var client = httpClientFactory.CreateClient("RottenTomatoes");

        try
        {
            using var response = await client.GetAsync(_discoverOptions.PopularSourceUrl, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning("Rotten Tomatoes popular page fetch failed with status {StatusCode}.", response.StatusCode);
                throw new ExternalServiceUnavailableException("Rotten Tomatoes is currently unavailable. Please try again shortly.");
            }

            var html = await response.Content.ReadAsStringAsync(cancellationToken);
            var entries = ParsePopularShowEntries(html);
            if (entries.Count == 0)
            {
                logger.LogWarning("Rotten Tomatoes popular page parsing returned zero entries.");
                throw new ExternalServiceUnavailableException("Rotten Tomatoes popular list could not be parsed.");
            }

            return entries;
        }
        catch (TaskCanceledException ex)
        {
            logger.LogWarning(ex, "Rotten Tomatoes popular page request timed out.");
            throw new ExternalServiceUnavailableException("Rotten Tomatoes request timed out. Please try again shortly.");
        }
        catch (HttpRequestException ex)
        {
            logger.LogWarning(ex, "Rotten Tomatoes network error.");
            throw new ExternalServiceUnavailableException("Rotten Tomatoes is currently unavailable. Please try again shortly.");
        }
    }

    private static IReadOnlyList<RottenTomatoesPopularEntry> ParsePopularShowEntries(string html)
    {
        return CountdownEntryRegex.Matches(html)
            .Select(match => new RottenTomatoesPopularEntry
            {
                Rank = int.TryParse(match.Groups["rank"].Value, out var rank) ? rank : int.MaxValue,
                Title = CleanTitle(match.Groups["title"].Value),
                YearHint = ParseYearHint(match.Groups["href"].Value)
            })
            .Where(entry => entry.Rank > 0 && entry.Rank <= 25)
            .Where(entry => !string.IsNullOrWhiteSpace(entry.Title))
            .DistinctBy(entry => entry.Rank)
            .OrderBy(entry => entry.Rank)
            .ToList();
    }

    private static string CleanTitle(string rawTitle)
    {
        var withoutTags = HtmlTagRegex.Replace(rawTitle, string.Empty);
        var decodedTitle = WebUtility.HtmlDecode(withoutTags).Trim();
        return SeasonSuffixRegex.Replace(decodedTitle, string.Empty).Trim();
    }

    private static int? ParseYearHint(string href)
    {
        var match = YearHintRegex.Match(href);
        return match.Success && int.TryParse(match.Groups["year"].Value, out var year)
            ? year
            : null;
    }
}