namespace Wbijam.WebScrapper.Web;

public class AnimeModel
{
    public string Title { get; internal set; } = null!;
    internal List<AnimeSeries> Series { get; set; } = new();
}