namespace DollyZoomd.Options;

public class DiscoverOptions
{
    public const string SectionName = "Discover";

    public int PopularCacheTtlHours { get; set; } = 168;
    public int AllTimeGreatsCacheTtlHours { get; set; } = 168;
    public string[] AllTimeGreatsTitles { get; set; } = [];
    public int[] AllTimeGreatsTvMazeIds { get; set; } = [];
}
