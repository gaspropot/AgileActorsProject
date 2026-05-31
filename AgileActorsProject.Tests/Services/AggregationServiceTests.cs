using Microsoft.Extensions.Logging;
using AgileActorsProject.Application.DTOs;
using AgileActorsProject.Application.Interfaces;
using AgileActorsProject.Application.Models;
using AgileActorsProject.Application.Services;
using AgileActorsProject.Domain.Common;
using AgileActorsProject.Domain.Entities;
using AgileActorsProject.Domain.Interfaces;
using AgileActorsProject.Domain.Models;
using Moq;

namespace AgileActorsProject.Tests.Services;

public class AggregationServiceTests
{
    private readonly Mock<IStatisticsService> _statisticsServiceMock;
    private readonly Mock<ICacheService> _cacheServiceMock;
    private readonly Mock<ILogger<AggregationService>> _loggerMock;

    public AggregationServiceTests()
    {
        _statisticsServiceMock = new Mock<IStatisticsService>();
        _cacheServiceMock = new Mock<ICacheService>();
        _loggerMock = new Mock<ILogger<AggregationService>>();
    }

    private AggregationService CreateService(IEnumerable<IDataProvider> providers)
    {
        return new AggregationService(
            providers,
            _statisticsServiceMock.Object,
            _cacheServiceMock.Object,
            _loggerMock.Object);
    }

    private static void SetupCacheBypass(Mock<ICacheService> cacheServiceMock)
    {
        cacheServiceMock
            .Setup(c => c.GetOrSetAsync(
                It.IsAny<string>(),
                It.IsAny<Func<Task<IEnumerable<AggregatedItemDto>?>>>(),
                It.IsAny<TimeSpan>(),
                It.IsAny<CancellationToken>()))
            .Returns(async (string _, Func<Task<IEnumerable<AggregatedItemDto>?>> factory, TimeSpan _, CancellationToken _)
                => await factory());
    }

    [Fact]
    public async Task GetAggregatedDataAsync_AllProvidersSucceed_ReturnsAllItems()
    {
        // Arrange
        SetupCacheBypass(_cacheServiceMock);

        var providerA = new Mock<IDataProvider>();
        providerA.Setup(p => p.ProviderName).Returns("ProviderA");
        providerA.Setup(p => p.FetchAsync(It.IsAny<DataProviderRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<IEnumerable<AggregatedItem>>.Success(new[]
            {
            new AggregatedItem { Title = "Item A", Source = "ProviderA", Timestamp = DateTime.UtcNow }
            }));

        var providerB = new Mock<IDataProvider>();
        providerB.Setup(p => p.ProviderName).Returns("ProviderB");
        providerB.Setup(p => p.FetchAsync(It.IsAny<DataProviderRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<IEnumerable<AggregatedItem>>.Success(new[]
            {
            new AggregatedItem { Title = "Item B", Source = "ProviderB", Timestamp = DateTime.UtcNow }
            }));

        var service = CreateService(new[] { providerA.Object, providerB.Object });

        // Act
        var result = await service.GetAggregatedDataAsync(new AggregationQuery());

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value!.Count());
    }

    [Fact]
    public async Task GetAggregatedDataAsync_OneProviderFails_ReturnsItemsFromSuccessfulProvider()
    {
        // Arrange
        SetupCacheBypass(_cacheServiceMock);

        var successProvider = new Mock<IDataProvider>();
        successProvider.Setup(p => p.ProviderName).Returns("SuccessProvider");
        successProvider.Setup(p => p.FetchAsync(It.IsAny<DataProviderRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<IEnumerable<AggregatedItem>>.Success(new[]
            {
            new AggregatedItem { Title = "Item A", Source = "SuccessProvider", Timestamp = DateTime.UtcNow }
            }));

        var failingProvider = new Mock<IDataProvider>();
        failingProvider.Setup(p => p.ProviderName).Returns("FailingProvider");
        failingProvider.Setup(p => p.FetchAsync(It.IsAny<DataProviderRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<IEnumerable<AggregatedItem>>.Failure("Provider unavailable."));

        var service = CreateService(new[] { successProvider.Object, failingProvider.Object });

        // Act
        var result = await service.GetAggregatedDataAsync(new AggregationQuery());

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Single(result.Value!);
        Assert.Equal("Item A", result.Value!.First().Title);
    }

    [Fact]
    public async Task GetAggregatedDataAsync_AllProvidersFail_ReturnsFailure()
    {
        // Arrange
        SetupCacheBypass(_cacheServiceMock);

        var providerA = new Mock<IDataProvider>();
        providerA.Setup(p => p.ProviderName).Returns("ProviderA");
        providerA.Setup(p => p.FetchAsync(It.IsAny<DataProviderRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<IEnumerable<AggregatedItem>>.Failure("Provider A unavailable."));

        var providerB = new Mock<IDataProvider>();
        providerB.Setup(p => p.ProviderName).Returns("ProviderB");
        providerB.Setup(p => p.FetchAsync(It.IsAny<DataProviderRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<IEnumerable<AggregatedItem>>.Failure("Provider B unavailable."));

        var service = CreateService(new[] { providerA.Object, providerB.Object });

        // Act
        var result = await service.GetAggregatedDataAsync(new AggregationQuery());

        // Assert
        Assert.False(result.IsSuccess);
        Assert.NotNull(result.Error);
    }

    [Fact]
    public async Task GetAggregatedDataAsync_WithCategoryFilter_ReturnsOnlyMatchingItems()
    {
        // Arrange
        SetupCacheBypass(_cacheServiceMock);

        var provider = new Mock<IDataProvider>();
        provider.Setup(p => p.ProviderName).Returns("Provider");
        provider.Setup(p => p.FetchAsync(It.IsAny<DataProviderRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<IEnumerable<AggregatedItem>>.Success(new[]
            {
            new AggregatedItem { Title = "News Item", Category = "News", Timestamp = DateTime.UtcNow },
            new AggregatedItem { Title = "Weather Item", Category = "Weather", Timestamp = DateTime.UtcNow },
            new AggregatedItem { Title = "Dev Item", Category = "Development", Timestamp = DateTime.UtcNow }
            }));

        var service = CreateService(new[] { provider.Object });
        var query = new AggregationQuery { Category = "News" };

        // Act
        var result = await service.GetAggregatedDataAsync(query);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Single(result.Value!);
        Assert.Equal("News Item", result.Value!.First().Title);
    }

    [Fact]
    public async Task GetAggregatedDataAsync_WithDateRangeFilter_ReturnsOnlyItemsInRange()
    {
        // Arrange
        SetupCacheBypass(_cacheServiceMock);

        var now = DateTime.UtcNow;

        var provider = new Mock<IDataProvider>();
        provider.Setup(p => p.ProviderName).Returns("Provider");
        provider.Setup(p => p.FetchAsync(It.IsAny<DataProviderRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<IEnumerable<AggregatedItem>>.Success(new[]
            {
            new AggregatedItem { Title = "Old Item", Timestamp = now.AddDays(-10) },
            new AggregatedItem { Title = "Recent Item", Timestamp = now.AddDays(-2) },
            new AggregatedItem { Title = "Future Item", Timestamp = now.AddDays(1) }
            }));

        var service = CreateService(new[] { provider.Object });
        var query = new AggregationQuery
        {
            From = now.AddDays(-3),
            To = now
        };

        // Act
        var result = await service.GetAggregatedDataAsync(query);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Single(result.Value!);
        Assert.Equal("Recent Item", result.Value!.First().Title);
    }

    [Fact]
    public async Task GetAggregatedDataAsync_SortByTimestampDescending_ReturnsItemsInCorrectOrder()
    {
        // Arrange
        SetupCacheBypass(_cacheServiceMock);

        var now = DateTime.UtcNow;

        var provider = new Mock<IDataProvider>();
        provider.Setup(p => p.ProviderName).Returns("Provider");
        provider.Setup(p => p.FetchAsync(It.IsAny<DataProviderRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<IEnumerable<AggregatedItem>>.Success(new[]
            {
            new AggregatedItem { Title = "Oldest", Timestamp = now.AddDays(-3) },
            new AggregatedItem { Title = "Newest", Timestamp = now },
            new AggregatedItem { Title = "Middle", Timestamp = now.AddDays(-1) }
            }));

        var service = CreateService(new[] { provider.Object });
        var query = new AggregationQuery { SortBy = "timestamp", SortOrder = "desc" };

        // Act
        var result = await service.GetAggregatedDataAsync(query);

        // Assert
        Assert.True(result.IsSuccess);
        var items = result.Value!.ToList();
        Assert.Equal("Newest", items[0].Title);
        Assert.Equal("Middle", items[1].Title);
        Assert.Equal("Oldest", items[2].Title);
    }

    [Fact]
    public async Task GetAggregatedDataAsync_SortByRelevanceAscending_ReturnsItemsInCorrectOrder()
    {
        // Arrange
        SetupCacheBypass(_cacheServiceMock);

        var provider = new Mock<IDataProvider>();
        provider.Setup(p => p.ProviderName).Returns("Provider");
        provider.Setup(p => p.FetchAsync(It.IsAny<DataProviderRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<IEnumerable<AggregatedItem>>.Success(new[]
            {
            new AggregatedItem { Title = "High", RelevanceScore = 0.9, Timestamp = DateTime.UtcNow },
            new AggregatedItem { Title = "Low", RelevanceScore = 0.1, Timestamp = DateTime.UtcNow },
            new AggregatedItem { Title = "Mid", RelevanceScore = 0.5, Timestamp = DateTime.UtcNow }
            }));

        var service = CreateService(new[] { provider.Object });
        var query = new AggregationQuery { SortBy = "relevance", SortOrder = "asc" };

        // Act
        var result = await service.GetAggregatedDataAsync(query);

        // Assert
        Assert.True(result.IsSuccess);
        var items = result.Value!.ToList();
        Assert.Equal("Low", items[0].Title);
        Assert.Equal("Mid", items[1].Title);
        Assert.Equal("High", items[2].Title);
    }

    [Fact]
    public async Task GetAggregatedDataAsync_ProviderThrowsException_IsHandledGracefully()
    {
        // Arrange
        SetupCacheBypass(_cacheServiceMock);

        var throwingProvider = new Mock<IDataProvider>();
        throwingProvider.Setup(p => p.ProviderName).Returns("ThrowingProvider");
        throwingProvider.Setup(p => p.FetchAsync(It.IsAny<DataProviderRequest>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Unexpected error"));

        var goodProvider = new Mock<IDataProvider>();
        goodProvider.Setup(p => p.ProviderName).Returns("GoodProvider");
        goodProvider.Setup(p => p.FetchAsync(It.IsAny<DataProviderRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<IEnumerable<AggregatedItem>>.Success(new[]
            {
            new AggregatedItem { Title = "Good Item", Timestamp = DateTime.UtcNow }
            }));

        var service = CreateService(new[] { throwingProvider.Object, goodProvider.Object });

        // Act
        var result = await service.GetAggregatedDataAsync(new AggregationQuery());

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Single(result.Value!);
        Assert.Equal("Good Item", result.Value!.First().Title);
    }

    [Fact]
    public async Task GetAggregatedDataAsync_RecordsStatisticsForEachProvider()
    {
        // Arrange
        SetupCacheBypass(_cacheServiceMock);

        var providerA = new Mock<IDataProvider>();
        providerA.Setup(p => p.ProviderName).Returns("ProviderA");
        providerA.Setup(p => p.FetchAsync(It.IsAny<DataProviderRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<IEnumerable<AggregatedItem>>.Success(Enumerable.Empty<AggregatedItem>()));

        var providerB = new Mock<IDataProvider>();
        providerB.Setup(p => p.ProviderName).Returns("ProviderB");
        providerB.Setup(p => p.FetchAsync(It.IsAny<DataProviderRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<IEnumerable<AggregatedItem>>.Success(Enumerable.Empty<AggregatedItem>()));

        var service = CreateService(new[] { providerA.Object, providerB.Object });

        // Act
        await service.GetAggregatedDataAsync(new AggregationQuery());

        // Assert
        _statisticsServiceMock.Verify(
            s => s.RecordRequest("ProviderA", It.IsAny<double>()), Times.Once);
        _statisticsServiceMock.Verify(
            s => s.RecordRequest("ProviderB", It.IsAny<double>()), Times.Once);
    }
}
