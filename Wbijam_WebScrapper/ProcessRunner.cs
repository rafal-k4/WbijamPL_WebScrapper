using Serilog;

namespace Wbijam_WebScrapper;

public class ProcessRunner : IProcessRunner
{
    private readonly ILogger _logger;

    public ProcessRunner(ILogger logger)
    {
        _logger = logger;
    }

    public async Task RunAsync()
    {
        _logger.Information("hello from runner");
    }
}
