using Microsoft.Extensions.DependencyInjection;
using Serilog;
using Wbijam.WebScrapper;
using Wbijam.WebScrapper.File;
using Wbijam.WebScrapper.Web;

ILogger logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .WriteTo.Console()
    .WriteTo.File("logs/log.txt", rollingInterval: RollingInterval.Day)
    .CreateLogger();

var serviceCollection = new ServiceCollection();

serviceCollection.AddSingleton(logger);
serviceCollection.AddScoped<IProcessRunner, ProcessRunner>();
serviceCollection.AddScoped<IWebScrapper, WebScrapper>();
serviceCollection.AddScoped<IResultRecorder, ResultRecorder>();

var serviceProvider = serviceCollection.BuildServiceProvider();

await using var scope = serviceProvider.CreateAsyncScope();
var processRunner = scope.ServiceProvider.GetRequiredService<IProcessRunner>();

try
{
    await processRunner.RunAsync();
}
catch (Exception ex)
{
    logger.Error(ex, ex.Message);
}

