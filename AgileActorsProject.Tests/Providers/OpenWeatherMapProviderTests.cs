using Moq;
using System.Net;
using AgileActorsProject.Domain.Models;
using AgileActorsProject.Infrastructure.Providers.OpenWeatherMap;
using AgileActorsProject.Infrastructure.Settings;
using AgileActorsProject.Tests.TestHelpers;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AgileActorsProject.Tests.Providers;

public class OpenWeatherMapProviderTests
{
    private readonly Mock<ILogger<OpenWeatherMapProvider>> _loggerMock;
    private readonly Mock<IOptions<OpenWeatherMapSettings>> _settingsMock;

    private const string ValidResponse = """
        {
            "weather": [{ "main": "Clear", "description": "clear sky" }],
            "main": { "temp": 25.0, "feels_like": 24.0, "humidity": 60 },
            "wind": { "speed": 3.5 },
            "name": "Athens",
            "dt": 1717161600
        }
        """;

    private const string EmptyResponse = "null";

    public OpenWeatherMapProviderTests()
    {
        _loggerMock = new Mock<ILogger<OpenWeatherMapProvider>>();
        _settingsMock = new Mock<IOptions<OpenWeatherMapSettings>>();
        _settingsMock.Setup(s => s.Value).Returns(new OpenWeatherMapSettings
        {
            ApiKey = "test-api-key",
            DefaultCity = "Athens",
            BaseUrl = "https://api.openweathermap.org/data/2.5/"
        });
    }

    private OpenWeatherMapProvider CreateProvider(HttpStatusCode statusCode, string responseContent)
    {
        var httpClient = HttpClientFactory.Create(
            statusCode,
            responseContent,
            "https://api.openweathermap.org/data/2.5/");

        return new OpenWeatherMapProvider(httpClient, _settingsMock.Object, _loggerMock.Object);
    }

    [Fact]
    public async Task FetchAsync_SuccessResponse_ReturnsSingleAggregatedItem()
    {
        // Arrange
        var provider = CreateProvider(HttpStatusCode.OK, ValidResponse);

        // Act
        var result = await provider.FetchAsync(new DataProviderRequest());

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Single(result.Value!);
    }

    [Fact]
    public async Task FetchAsync_SuccessResponse_MapsFieldsCorrectly()
    {
        // Arrange
        var provider = CreateProvider(HttpStatusCode.OK, ValidResponse);

        // Act
        var result = await provider.FetchAsync(new DataProviderRequest());
        var item = result.Value!.First();

        // Assert
        Assert.Equal("Weather in Athens", item.Title);
        Assert.Equal("OpenWeatherMap", item.Source);
        Assert.Equal("Weather", item.Category);
        Assert.Contains("clear sky", item.Summary);
        Assert.Contains("25", item.Summary);
    }

    [Fact]
    public async Task FetchAsync_SuccessResponse_MetadataContainsExpectedKeys()
    {
        // Arrange
        var provider = CreateProvider(HttpStatusCode.OK, ValidResponse);

        // Act
        var result = await provider.FetchAsync(new DataProviderRequest());
        var item = result.Value!.First();

        // Assert
        Assert.True(item.Metadata.ContainsKey("temperature"));
        Assert.True(item.Metadata.ContainsKey("humidity"));
        Assert.True(item.Metadata.ContainsKey("windSpeed"));
        Assert.True(item.Metadata.ContainsKey("feelsLike"));
        Assert.True(item.Metadata.ContainsKey("condition"));
    }

    [Fact]
    public async Task FetchAsync_UsesKeywordAsCity_WhenKeywordProvided()
    {
        // Arrange
        var provider = CreateProvider(HttpStatusCode.OK, ValidResponse);
        var request = new DataProviderRequest { Keyword = "London" };

        // Act
        var result = await provider.FetchAsync(request);

        // Assert
        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task FetchAsync_NonSuccessStatusCode_ReturnsFailure()
    {
        // Arrange
        var provider = CreateProvider(HttpStatusCode.Unauthorized, string.Empty);

        // Act
        var result = await provider.FetchAsync(new DataProviderRequest());

        // Assert
        Assert.False(result.IsSuccess);
        Assert.NotNull(result.Error);
    }

    [Fact]
    public async Task FetchAsync_NullResponseBody_ReturnsFailure()
    {
        // Arrange
        var provider = CreateProvider(HttpStatusCode.OK, EmptyResponse);

        // Act
        var result = await provider.FetchAsync(new DataProviderRequest());

        // Assert
        Assert.False(result.IsSuccess);
        Assert.NotNull(result.Error);
    }

    [Fact]
    public async Task FetchAsync_HttpRequestException_ReturnsFailure()
    {
        // Arrange — handler that throws
        var handler = new ThrowingHttpMessageHandler(new HttpRequestException("Network error"));
        var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://api.openweathermap.org/data/2.5/")
        };
        var provider = new OpenWeatherMapProvider(httpClient, _settingsMock.Object, _loggerMock.Object);

        // Act
        var result = await provider.FetchAsync(new DataProviderRequest());

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Contains("unavailable", result.Error, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task FetchAsync_Timeout_ReturnsFailure()
    {
        // Arrange — handler that simulates timeout
        var handler = new ThrowingHttpMessageHandler(new TaskCanceledException("Timeout"));
        var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://api.openweathermap.org/data/2.5/")
        };
        var provider = new OpenWeatherMapProvider(httpClient, _settingsMock.Object, _loggerMock.Object);

        // Act
        var result = await provider.FetchAsync(new DataProviderRequest());

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Contains("timed out", result.Error, StringComparison.OrdinalIgnoreCase);
    }
}
