namespace Wbijam.WebScrapper.Web;

public interface IWebScrapper
{
    Task<AnimeModel> GetAnimeData();
}
