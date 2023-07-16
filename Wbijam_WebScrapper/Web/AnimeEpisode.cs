namespace Wbijam.WebScrapper.Web
{
    public class AnimeEpisode
    {
        public string EpisodeName { get; set; } = null!;
        public string EpisodeType { get; set; } = null!;
        public string RangeOfOpeningOrEndings { get; internal set; } = null!;
        public string EpisodeReleaseDate { get; set; }

        public string EpisodePlayersUrlPath { get; set; } = null!;
        public List<string> EpisodeVideoUrls { get; set; } = new();
        
    }
}