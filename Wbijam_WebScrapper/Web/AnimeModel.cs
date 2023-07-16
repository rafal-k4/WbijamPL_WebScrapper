namespace Wbijam.WebScrapper.Web;

public class AnimeModel
{
    public string Title { get; set; } = null!;
    public List<AnimeSeries> Series { get; set; } = new();
}