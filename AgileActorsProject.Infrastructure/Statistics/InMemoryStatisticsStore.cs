using AgileActorsProject.Domain.Interfaces;
using System.Collections.Concurrent;


namespace AgileActorsProject.Infrastructure.Statistics;

public class InMemoryStatisticsStore
{
    private readonly ConcurrentDictionary<string, ProviderStats> _stats = new();
    private readonly IDateTimeProvider _dateTimeProvider;

    public InMemoryStatisticsStore(IDateTimeProvider dateTimeProvider)
    {
        _dateTimeProvider = dateTimeProvider;
    }

    public void Record(string providerName, double responseTimeMs)
    {
        var stats = _stats.GetOrAdd(providerName, name => new ProviderStats(name));
        stats.Record(responseTimeMs, _dateTimeProvider.UtcNow);
    }

    public IEnumerable<ProviderStats> GetAll() => _stats.Values;

    public IEnumerable<(string ProviderName, double AverageResponseTimeMs)> GetRecentAverages(TimeSpan window)
    {
        var cutoff = _dateTimeProvider.UtcNow - window;
        return _stats.Select(kvp => (
            kvp.Key,
            kvp.Value.GetAverageResponseTimeSince(cutoff)
        ));
    }
}
