using Microsoft.Extensions.DependencyInjection;
using Serilog;
using Wbijam_WebScrapper;

ILogger logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .WriteTo.Console()
    .WriteTo.File("logs/log.txt", rollingInterval: RollingInterval.Day)
    .CreateLogger();

var serviceCollection = new ServiceCollection();

serviceCollection.AddSingleton(logger);
serviceCollection.AddSingleton<IProcessRunner, ProcessRunner>();

var serviceProvider = serviceCollection.BuildServiceProvider();

var processRunner = serviceProvider.GetRequiredService<IProcessRunner>();

try
{
    await processRunner.RunAsync();
}
catch (Exception ex)
{
    logger.Error(ex, ex.Message);
}

