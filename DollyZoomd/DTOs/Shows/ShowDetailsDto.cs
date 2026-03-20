namespace DollyZoomd.DTOs.Shows;

public class ShowDetailsDto
{
    public int TvMazeId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? PosterUrl { get; set; }
    public string? BannerUrl { get; set; }
    public string? SummaryHtml { get; set; }
    public IReadOnlyList<string> Genres { get; set; } = Array.Empty<string>();
    public double? AverageRating { get; set; }
    public string? NetworkName { get; set; }
    public string? Status { get; set; }
    public DateOnly? PremieredOn { get; set; }
    public DateOnly? EndedOn { get; set; }
    public IReadOnlyList<ShowDetailsEpisodeDto> Episodes { get; set; } = Array.Empty<ShowDetailsEpisodeDto>();
    public IReadOnlyList<ShowDetailsCastMemberDto> Cast { get; set; } = Array.Empty<ShowDetailsCastMemberDto>();
}

public class ShowDetailsEpisodeDto
{
    public int EpisodeId { get; set; }
    public string Name { get; set; } = string.Empty;
    public int Season { get; set; }
    public int Number { get; set; }
    public DateOnly? AirDate { get; set; }
    public string? SummaryHtml { get; set; }
    public string? ThumbnailUrl { get; set; }
}

public class ShowDetailsCastMemberDto
{
    public int PersonId { get; set; }
    public string PersonName { get; set; } = string.Empty;
    public string CharacterName { get; set; } = string.Empty;
    public string? PersonImageUrl { get; set; }
}
