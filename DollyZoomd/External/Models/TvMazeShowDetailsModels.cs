namespace DollyZoomd.External.Models;

public class TvMazeEpisode
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int Season { get; set; }
    public int Number { get; set; }
    public string? Airdate { get; set; }
    public string? Summary { get; set; }
    public TvMazeImage? Image { get; set; }
}

public class TvMazeCastMember
{
    public TvMazePerson? Person { get; set; }
    public TvMazeCharacter? Character { get; set; }
}

public class TvMazePerson
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public TvMazeImage? Image { get; set; }
}

public class TvMazeCharacter
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public TvMazeImage? Image { get; set; }
}
