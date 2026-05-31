using AgileActorsProject.Infrastructure.Services;
using AgileActorsProject.Infrastructure.Settings;
using AgileActorsProject.Infrastructure.Statistics;
using AgileActorsProject.Tests.TestHelpers;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;

namespace AgileActorsProject.Tests.Services;

public class AnomalyDetectionServiceTests
{
    private readonly MockDateTimeProvider _mockDateTimeProvider;
    private readonly InMemoryStatisticsStore _store;
    private readonly Mock<ILogger<AnomalyDetectionService>> _loggerMock;
    private readonly Mock<IOptions<AnomalyDetectionSettings>> _settingsMock;
    private readonly AnomalyDetectionService _service;

    public AnomalyDetectionServiceTests()
    {
        _mockDateTimeProvider = new MockDateTimeProvider(DateTime.UtcNow);
        _store = new InMemoryStatisticsStore(_mockDateTimeProvider);
        _loggerMock = new Mock<ILogger<AnomalyDetectionService>>();
        _settingsMock = new Mock<IOptions<AnomalyDetectionSettings>>();
        _settingsMock.Setup(s => s.Value).Returns(new AnomalyDetectionSettings
        {
            IntervalSeconds = 30,
            AnomalyThresholdPercent = 50.0
        });

        _service = new AnomalyDetectionService(_store, _loggerMock.Object, _settingsMock.Object);
    }

    [Fact]
    public async Task DetectAndLogAnomaliesAsync_NoStats_DoesNotLog()
    {
        // Act
        await _service.DetectAndLogAnomaliesAsync();

        // Assert
        _loggerMock.Verify(
            l => l.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Never);
    }

    [Fact]
    public async Task DetectAndLogAnomaliesAsync_RecentAverageWellBelowThreshold_DoesNotLog()
    {
        // Arrange — record normal requests in the past
        for (int i = 0; i < 10; i++)
            _store.Record("ProviderA", 500.0);

        // Advance time so past requests fall outside the 5 minute window
        _mockDateTimeProvider.Advance(TimeSpan.FromMinutes(10));

        // Record recent requests also at normal speed
        for (int i = 0; i < 5; i++)
            _store.Record("ProviderA", 550.0);

        // Act
        await _service.DetectAndLogAnomaliesAsync();

        // Assert
        _loggerMock.Verify(
            l => l.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Never);
    }

    [Fact]
    public async Task DetectAndLogAnomaliesAsync_RecentAverageAboveThreshold_LogsWarning()
    {
        // Arrange — record old normal requests
        for (int i = 0; i < 10; i++)
            _store.Record("ProviderA", 500.0);

        // Advance time beyond the 5 minute window
        _mockDateTimeProvider.Advance(TimeSpan.FromMinutes(10));

        // Record recent slow requests
        for (int i = 0; i < 5; i++)
            _store.Record("ProviderA", 2000.0);

        // Act
        await _service.DetectAndLogAnomaliesAsync();

        // Assert
        _loggerMock.Verify(
            l => l.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task DetectAndLogAnomaliesAsync_MultipleProviders_LogsOnlyThoseWithAnomaly()
    {
        // Arrange — ProviderA normal throughout
        for (int i = 0; i < 10; i++)
            _store.Record("ProviderA", 500.0);

        // ProviderB normal in the past
        for (int i = 0; i < 10; i++)
            _store.Record("ProviderB", 500.0);

        // Advance time beyond the 5 minute window
        _mockDateTimeProvider.Advance(TimeSpan.FromMinutes(10));

        // ProviderA still normal recently
        for (int i = 0; i < 5; i++)
            _store.Record("ProviderA", 520.0);

        // ProviderB spikes recently
        for (int i = 0; i < 5; i++)
            _store.Record("ProviderB", 2000.0);

        // Act
        await _service.DetectAndLogAnomaliesAsync();

        // Assert
        _loggerMock.Verify(
            l => l.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task DetectAndLogAnomaliesAsync_ProviderWithZeroOverallAverage_DoesNotLog()
    {
        // Arrange
        _store.Record("ProviderA", 0.0);

        // Act
        await _service.DetectAndLogAnomaliesAsync();

        // Assert
        _loggerMock.Verify(
            l => l.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Never);
    }
}