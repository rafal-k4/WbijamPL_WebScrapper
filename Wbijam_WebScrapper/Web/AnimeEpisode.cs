namespace Wbijam.WebScrapper.Web;

public class AnimeEpisode
{
    public string EpisodeName { get; set; } = null!;
    public string EpisodeType { get; set; } = null!;
    public string? EpisodeReleaseDateOrRangeOfEpisodes { get; set; }

    internal string EpisodePlayersUrlPath { get; set; } = null!;
    internal List<string> PlayersUrlPaths { get; set; } = new();
    public List<string> EpisodeVideoUrls { get; set; } = new();
    
}