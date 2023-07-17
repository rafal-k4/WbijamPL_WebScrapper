namespace Wbijam.WebScrapper.Web;

public interface IWebScrapper: IAsyncDisposable
{
    Task GetAnimeDataAsync(Func<AnimeModel, Task> scrappedAnimeDelegate);
}
