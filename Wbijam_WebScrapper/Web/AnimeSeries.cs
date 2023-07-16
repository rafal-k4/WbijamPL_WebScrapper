namespace Wbijam.WebScrapper.Web;

public class AnimeSeries
{
    public string SeriesName { get; set; } = null!;
    public string SeriesUrlPath { get; set; } = null!;

    public List<AnimeEpisode> AnimeEpisodes { get; set; } = new();
}