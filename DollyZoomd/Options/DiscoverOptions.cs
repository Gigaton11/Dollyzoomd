namespace DollyZoomd.Options;

public class DiscoverOptions
{
    public const string SectionName = "Discover";

    public string PopularSourceUrl { get; set; } = "https://editorial.rottentomatoes.com/guide/popular-tv-shows/";
    public int PopularRefreshCheckHours { get; set; } = 24;
    public int PopularCacheTtlHours { get; set; } = 240;
    public int AllTimeGreatsCacheTtlHours { get; set; } = 168;
    public string[] AllTimeGreatsTitles { get; set; } = [];
    public int[] AllTimeGreatsTvMazeIds { get; set; } = [];
}
