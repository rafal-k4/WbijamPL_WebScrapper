namespace Wbijam.WebScrapper.Web;

public class AnimeModel
{
    public string Title { get; set; } = null!;

    internal List<string> SeriesPagesPaths { get; set; } = new();
    public List<AnimeSeries> Series { get; set; } = new();
}