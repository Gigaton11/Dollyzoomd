namespace DollyZoomd.External.Models;

public class TvMazeSearchResult
{
    public TvMazeShow? Show { get; set; }
}

public class TvMazeShow
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public List<string>? Genres { get; set; }
    public string? Premiered { get; set; }
    public string? Ended { get; set; }
    public string? Status { get; set; }
    public string? Summary { get; set; }
    public TvMazeImage? Image { get; set; }
    public TvMazeRating? Rating { get; set; }
    public TvMazeNetwork? Network { get; set; }
    public TvMazeNetwork? WebChannel { get; set; }
}

public class TvMazeImage
{
    public string? Medium { get; set; }
    public string? Original { get; set; }
}

public class TvMazeRating
{
    public double? Average { get; set; }
}

public class TvMazeNetwork
{
    public string? Name { get; set; }
}
