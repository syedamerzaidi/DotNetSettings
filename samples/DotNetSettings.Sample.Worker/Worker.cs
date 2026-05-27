using DotNetSettings.Sample.Worker.Settings;
using Microsoft.Extensions.DependencyInjection;

namespace DotNetSettings.Sample.Worker;

public class Worker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<Worker> _logger;

    public Worker(IServiceScopeFactory scopeFactory, ILogger<Worker> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            using var scope = _scopeFactory.CreateScope();
            var settings = scope.ServiceProvider.GetRequiredService<WorkerSettings>();

            if (!settings.ProcessingEnabled)
            {
                _logger.LogInformation("Processing disabled — skipping tick");
            }
            else
            {
                _logger.LogInformation(
                    "Processing batch of {BatchSize} items",
                    settings.BatchSize);
            }

            var delay = TimeSpan.FromSeconds(settings.PollIntervalSeconds);
            await Task.Delay(delay, stoppingToken);
        }
    }
}
