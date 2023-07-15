using Serilog;
using Wbijam.WebScrapper.Web;

namespace Wbijam.WebScrapper;

public class ProcessRunner : IProcessRunner
{
    private readonly IWebScrapper _webScrapper;

    public ProcessRunner(IWebScrapper webScrapper)
    {
        _webScrapper = webScrapper;
    }

    public async Task RunAsync()
    {
        var scrappedAnimes = await _webScrapper.GetAnimeDataAsync();
    }
}
