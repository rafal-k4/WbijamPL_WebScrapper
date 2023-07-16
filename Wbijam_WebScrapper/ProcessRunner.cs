using Humanizer;
using Newtonsoft.Json;
using Serilog;
using System.Diagnostics;
using Wbijam.WebScrapper.File;
using Wbijam.WebScrapper.Web;

namespace Wbijam.WebScrapper;

public class ProcessRunner : IProcessRunner
{
    private readonly IWebScrapper _webScrapper;
    private readonly ILogger _logger;
    private readonly IResultRecorder _resultRecorder;

    public ProcessRunner(
        IWebScrapper webScrapper,
        ILogger logger,
        IResultRecorder resultRecorder)
    {
        _webScrapper = webScrapper;
        _logger = logger;
        _resultRecorder = resultRecorder;
    }

    public async Task RunAsync()
    {
        var stopwatch = Stopwatch.StartNew();

        await _webScrapper.GetAnimeDataAsync(async anime =>
        {
            await _resultRecorder.SaveResult(anime);
        });
        
        stopwatch.Stop();
        _logger.Information("Whole process took: {elapsedTime}",
           TimeSpan.FromMilliseconds(stopwatch.ElapsedMilliseconds).Humanize(5, new System.Globalization.CultureInfo("en")));
    }
}
