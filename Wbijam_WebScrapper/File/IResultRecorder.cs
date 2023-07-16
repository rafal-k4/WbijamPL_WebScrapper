using Wbijam.WebScrapper.Web;

namespace Wbijam.WebScrapper.File;

public interface IResultRecorder
{
    Task SaveResult(List<AnimeModel> scrappedAnimes);
}
