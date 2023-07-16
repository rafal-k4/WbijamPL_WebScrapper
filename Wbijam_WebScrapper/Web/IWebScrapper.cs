namespace Wbijam.WebScrapper.Web;

public interface IWebScrapper
{
    Task GetAnimeDataAsync(Func<AnimeModel, Task> scrappedAnimeDelegate);
}
