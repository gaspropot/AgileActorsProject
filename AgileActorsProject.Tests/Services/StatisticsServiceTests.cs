using AgileActorsProject.Infrastructure.Services;
using AgileActorsProject.Infrastructure.Statistics;
using AgileActorsProject.Tests.TestHelpers;

namespace AgileActorsProject.Tests.Services;

public class StatisticsServiceTests
{
    private readonly InMemoryStatisticsStore _store;
    private readonly StatisticsService _service;

    public StatisticsServiceTests()
    {
        var mockDateTimeProvider = new MockDateTimeProvider(DateTime.UtcNow);
        _store = new InMemoryStatisticsStore(mockDateTimeProvider);
        _service = new StatisticsService(_store);
    }

    [Fact]
    public void RecordRequest_SingleRequest_TotalRequestsIsOne()
    {
        // Act
        _service.RecordRequest("ProviderA", 200.0);

        // Assert
        var stats = _service.GetStatistics().ToList();
        Assert.Single(stats);
        Assert.Equal(1, stats[0].TotalRequests);
    }

    [Fact]
    public void RecordRequest_MultipleRequestsSameProvider_TotalRequestsIsCorrect()
    {
        // Act
        _service.RecordRequest("ProviderA", 200.0);
        _service.RecordRequest("ProviderA", 400.0);
        _service.RecordRequest("ProviderA", 600.0);

        // Assert
        var stats = _service.GetStatistics().ToList();
        Assert.Single(stats);
        Assert.Equal(3, stats[0].TotalRequests);
    }

    [Fact]
    public void RecordRequest_MultipleProviders_CreatesStatsForEach()
    {
        // Act
        _service.RecordRequest("ProviderA", 200.0);
        _service.RecordRequest("ProviderB", 400.0);
        _service.RecordRequest("ProviderC", 600.0);

        // Assert
        var stats = _service.GetStatistics().ToList();
        Assert.Equal(3, stats.Count);
    }

    [Fact]
    public void RecordRequest_MultipleRequests_AverageResponseTimeIsCorrect()
    {
        // Act
        _service.RecordRequest("ProviderA", 200.0);
        _service.RecordRequest("ProviderA", 400.0);
        _service.RecordRequest("ProviderA", 600.0);

        // Assert
        var stats = _service.GetStatistics().Single();
        Assert.Equal(400.0, stats.AverageResponseTimeMs);
    }

    [Fact]
    public void RecordRequest_FastRequest_IncreasesFastBucket()
    {
        // Act
        _service.RecordRequest("ProviderA", 100.0);

        // Assert
        var stats = _service.GetStatistics().Single();
        Assert.Equal(1, stats.Buckets.Fast);
        Assert.Equal(0, stats.Buckets.Average);
        Assert.Equal(0, stats.Buckets.Slow);
    }

    [Fact]
    public void RecordRequest_AverageRequest_IncreasesAverageBucket()
    {
        // Act
        _service.RecordRequest("ProviderA", 800.0);

        // Assert
        var stats = _service.GetStatistics().Single();
        Assert.Equal(0, stats.Buckets.Fast);
        Assert.Equal(1, stats.Buckets.Average);
        Assert.Equal(0, stats.Buckets.Slow);
    }

    [Fact]
    public void RecordRequest_SlowRequest_IncreasesSlowBucket()
    {
        // Act
        _service.RecordRequest("ProviderA", 2000.0);

        // Assert
        var stats = _service.GetStatistics().Single();
        Assert.Equal(0, stats.Buckets.Fast);
        Assert.Equal(0, stats.Buckets.Average);
        Assert.Equal(1, stats.Buckets.Slow);
    }

    [Fact]
    public void RecordRequest_MixedRequests_BucketsAreCorrect()
    {
        // Act
        _service.RecordRequest("ProviderA", 100.0);  // fast
        _service.RecordRequest("ProviderA", 800.0);  // average
        _service.RecordRequest("ProviderA", 300.0);  // fast
        _service.RecordRequest("ProviderA", 1200.0); // average
        _service.RecordRequest("ProviderA", 2000.0); // slow

        // Assert
        var stats = _service.GetStatistics().Single();
        Assert.Equal(2, stats.Buckets.Fast);
        Assert.Equal(2, stats.Buckets.Average);
        Assert.Equal(1, stats.Buckets.Slow);
    }

    [Fact]
    public void GetStatistics_NoRequests_ReturnsEmptyCollection()
    {
        // Act
        var stats = _service.GetStatistics();

        // Assert
        Assert.Empty(stats);
    }

    [Fact]
    public void RecordRequest_ConcurrentRequests_TotalRequestsIsCorrect()
    {
        // Arrange
        const int requestCount = 100;

        // Act
        Parallel.For(0, requestCount, i =>
        {
            _service.RecordRequest("ProviderA", i * 10.0);
        });

        // Assert
        var stats = _service.GetStatistics().Single();
        Assert.Equal(requestCount, stats.TotalRequests);
    }
}
