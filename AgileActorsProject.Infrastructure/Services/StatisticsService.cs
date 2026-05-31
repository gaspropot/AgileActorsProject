using AgileActorsProject.Application.DTOs;
using AgileActorsProject.Application.Interfaces;
using AgileActorsProject.Infrastructure.Statistics;

namespace AgileActorsProject.Infrastructure.Services;

public class StatisticsService : IStatisticsService
{
    private readonly InMemoryStatisticsStore _store;

    public StatisticsService(InMemoryStatisticsStore store)
    {
        _store = store;
    }

    public void RecordRequest(string providerName, double responseTimeMs)
    {
        _store.Record(providerName, responseTimeMs);
    }

    public IEnumerable<ProviderStatisticsDto> GetStatistics()
    {
        return _store.GetAll().Select(s => new ProviderStatisticsDto
        {
            ProviderName = s.ProviderName,
            TotalRequests = s.TotalRequests,
            AverageResponseTimeMs = s.AverageResponseTimeMs,
            Buckets = new PerformanceBucketsDto
            {
                Fast = s.FastCount,
                Average = s.AverageCount,
                Slow = s.SlowCount
            }
        });
    }
}
