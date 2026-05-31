using System.Collections.Concurrent;

namespace AgileActorsProject.Infrastructure.Statistics;

public class ProviderStats
{
    public string ProviderName { get; }

    private readonly ConcurrentBag<RequestRecord> _records = new();

    public ProviderStats(string providerName)
    {
        ProviderName = providerName;
    }

    public void Record(double responseTimeMs, DateTime timestamp)
    {
        _records.Add(new RequestRecord(responseTimeMs, timestamp));
    }

    public int TotalRequests => _records.Count;

    public double AverageResponseTimeMs =>
        _records.IsEmpty ? 0 : _records.Average(r => r.ResponseTimeMs);

    public int FastCount => _records.Count(r => r.ResponseTimeMs < 500);
    public int AverageCount => _records.Count(r => r.ResponseTimeMs >= 500 && r.ResponseTimeMs <= 1500);
    public int SlowCount => _records.Count(r => r.ResponseTimeMs > 1500);

    public double GetAverageResponseTimeSince(DateTime cutoff)
    {
        var recent = _records.Where(r => r.Timestamp >= cutoff).ToList();
        return recent.Count == 0 ? 0 : recent.Average(r => r.ResponseTimeMs);
    }
}
