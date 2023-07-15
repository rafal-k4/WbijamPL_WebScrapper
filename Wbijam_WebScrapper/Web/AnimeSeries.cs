namespace Wbijam.WebScrapper.Web
{
    internal class AnimeSeries
    {
        public string SeriesName { get; set; } = null!;
        public string SeriesUrlPath { get; internal set; } = null!;

        public List<AnimeEpisode> AnimeEpisodes { get; set; } = new();
    }
}