using AgileActorsProject.Application.Interfaces;
using AgileActorsProject.Infrastructure.Settings;
using AgileActorsProject.Infrastructure.Statistics;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AgileActorsProject.Infrastructure.Services;

public class AnomalyDetectionService : IAnomalyDetectionService
{
    private readonly InMemoryStatisticsStore _store;
    private readonly ILogger<AnomalyDetectionService> _logger;
    private readonly AnomalyDetectionSettings _settings;
    private static readonly TimeSpan RecentWindow = TimeSpan.FromMinutes(5);

    public AnomalyDetectionService(
        InMemoryStatisticsStore store,
        ILogger<AnomalyDetectionService> logger,
        IOptions<AnomalyDetectionSettings> settings)
    {
        _store = store;
        _logger = logger;
        _settings = settings.Value;
    }

    public Task DetectAndLogAnomaliesAsync(CancellationToken cancellationToken = default)
    {
        var allStats = _store.GetAll().ToList();

        if (!allStats.Any())
            return Task.CompletedTask;

        var recentAverages = _store.GetRecentAverages(RecentWindow)
            .ToDictionary(x => x.ProviderName, x => x.AverageResponseTimeMs);

        foreach (var stats in allStats)
        {
            var overallAverage = stats.AverageResponseTimeMs;
            if (overallAverage == 0) continue;

            if (!recentAverages.TryGetValue(stats.ProviderName, out var recentAverage)) continue;
            if (recentAverage == 0) continue;

            var percentIncrease = ((recentAverage - overallAverage) / overallAverage) * 100.0;

            if (percentIncrease > _settings.AnomalyThresholdPercent)
            {
                _logger.LogWarning(
                    "Performance anomaly detected for {Provider}: " +
                    "recent average {RecentAvg:F2}ms is {Percent:F1}% above overall average {OverallAvg:F2}ms",
                    stats.ProviderName,
                    recentAverage,
                    percentIncrease,
                    overallAverage);
            }
        }

        return Task.CompletedTask;
    }
}
