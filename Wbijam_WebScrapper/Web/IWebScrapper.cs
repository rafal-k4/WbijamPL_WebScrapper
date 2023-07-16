namespace Wbijam.WebScrapper.Web;

public interface IWebScrapper
{
    Task<List<AnimeModel>> GetAnimeDataAsync();
}
