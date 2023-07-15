using Newtonsoft.Json;
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

        const string resultDir = @"C:/temp/";

        if (!Directory.Exists(resultDir))
            Directory.CreateDirectory(resultDir);

        File.WriteAllText(Path.Combine(resultDir, $"anime_list_{DateTime.Now:yyyyMMdd_HHmmss}.txt"), JsonConvert.SerializeObject(scrappedAnimes, Formatting.Indented));
    }
}
