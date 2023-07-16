using Wbijam.WebScrapper.Web;

namespace Wbijam.WebScrapper.File;

public interface IResultRecorder
{
    Task SaveResult(AnimeModel scrappedAnime);
}
