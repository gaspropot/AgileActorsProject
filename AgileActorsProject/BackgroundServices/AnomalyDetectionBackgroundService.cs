using AgileActorsProject.Application.Interfaces;
using AgileActorsProject.Infrastructure.Settings;
using Microsoft.Extensions.Options;

namespace AgileActorsProject.WebAPI.BackgroundServices;

public class AnomalyDetectionBackgroundService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<AnomalyDetectionBackgroundService> _logger;
    private readonly AnomalyDetectionSettings _settings;

    public AnomalyDetectionBackgroundService(
        IServiceScopeFactory scopeFactory,
        ILogger<AnomalyDetectionBackgroundService> logger,
        IOptions<AnomalyDetectionSettings> settings)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        _settings = settings.Value;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Anomaly detection background service started.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                // I delay this a bit because on startup there are basically no stats
                await Task.Delay(TimeSpan.FromSeconds(_settings.IntervalSeconds), stoppingToken);

                // Creating a new scope to resolve scoped services like IAnomalyDetectionService
                // This way we are safe from captive dependency issues
                using var scope = _scopeFactory.CreateScope();
                var anomalyDetectionService = scope.ServiceProvider
                    .GetRequiredService<IAnomalyDetectionService>();

                await anomalyDetectionService.DetectAndLogAnomaliesAsync(stoppingToken);
            }
            catch (OperationCanceledException)
            {
                // This is expected on shutdown, don't log as error
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unhandled error in anomaly detection background service.");
            }
        }

        _logger.LogInformation("Anomaly detection background service stopped.");
    }
}
