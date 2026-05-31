using AgileActorsProject.Domain.Models;
using AgileActorsProject.Infrastructure.Providers.NewsApi;
using AgileActorsProject.Tests.TestHelpers;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using System.Net;

namespace AgileActorsProject.Tests.Providers;

public class NewsApiProviderTests
{
    private readonly Mock<ILogger<NewsApiProvider>> _loggerMock;
    private readonly Mock<IOptions<NewsApiSettings>> _settingsMock;

    private const string ValidResponse = """
        {
            "status": "ok",
            "totalResults": 2,
            "articles": [
                {
                    "source": { "id": "bbc-news", "name": "BBC News" },
                    "title": "Test Article One",
                    "description": "Description One",
                    "url": "https://bbc.com/article-one",
                    "publishedAt": "2024-01-01T10:00:00Z",
                    "content": "Content One"
                },
                {
                    "source": { "id": "the-verge", "name": "The Verge" },
                    "title": "Test Article Two",
                    "description": "Description Two",
                    "url": "https://theverge.com/article-two",
                    "publishedAt": "2024-01-01T09:00:00Z",
                    "content": "Content Two"
                }
            ]
        }
        """;

    private const string EmptyArticlesResponse = """
        {
            "status": "ok",
            "totalResults": 0,
            "articles": []
        }
        """;

    private const string InvalidStatusResponse = """
        {
            "status": "error",
            "totalResults": 0,
            "articles": []
        }
        """;

    private const string RemovedArticlesResponse = """
        {
            "status": "ok",
            "totalResults": 2,
            "articles": [
                {
                    "source": { "id": "bbc-news", "name": "BBC News" },
                    "title": "[Removed]",
                    "description": "Removed",
                    "url": "https://bbc.com/removed",
                    "publishedAt": "2024-01-01T10:00:00Z",
                    "content": "Removed"
                },
                {
                    "source": { "id": "the-verge", "name": "The Verge" },
                    "title": "Valid Article",
                    "description": "Valid Description",
                    "url": "https://theverge.com/valid",
                    "publishedAt": "2024-01-01T09:00:00Z",
                    "content": "Valid Content"
                }
            ]
        }
        """;

    public NewsApiProviderTests()
    {
        _loggerMock = new Mock<ILogger<NewsApiProvider>>();
        _settingsMock = new Mock<IOptions<NewsApiSettings>>();
        _settingsMock.Setup(s => s.Value).Returns(new NewsApiSettings
        {
            ApiKey = "test-api-key",
            DefaultSources = "bbc-news,the-verge,reuters",
            BaseUrl = "https://newsapi.org/v2/"
        });
    }

    private NewsApiProvider CreateProvider(HttpStatusCode statusCode, string responseContent)
    {
        var httpClient = HttpClientFactory.Create(
            statusCode,
            responseContent,
            "https://newsapi.org/v2/");

        return new NewsApiProvider(httpClient, _settingsMock.Object, _loggerMock.Object);
    }

    [Fact]
    public async Task FetchAsync_SuccessResponse_ReturnsCorrectNumberOfItems()
    {
        // Arrange
        var provider = CreateProvider(HttpStatusCode.OK, ValidResponse);

        // Act
        var result = await provider.FetchAsync(new DataProviderRequest());

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value!.Count());
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
        Assert.Equal("Test Article One", item.Title);
        Assert.Equal("Description One", item.Summary);
        Assert.Equal("https://bbc.com/article-one", item.Url);
        Assert.Equal("NewsAPI", item.Source);
        Assert.Equal("News", item.Category);
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
        Assert.True(item.Metadata.ContainsKey("sourceName"));
        Assert.True(item.Metadata.ContainsKey("sourceId"));
        Assert.True(item.Metadata.ContainsKey("content"));
    }

    [Fact]
    public async Task FetchAsync_EmptyArticles_ReturnsSuccessWithEmptyCollection()
    {
        // Arrange
        var provider = CreateProvider(HttpStatusCode.OK, EmptyArticlesResponse);

        // Act
        var result = await provider.FetchAsync(new DataProviderRequest());

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Empty(result.Value!);
    }

    [Fact]
    public async Task FetchAsync_InvalidStatus_ReturnsFailure()
    {
        // Arrange
        var provider = CreateProvider(HttpStatusCode.OK, InvalidStatusResponse);

        // Act
        var result = await provider.FetchAsync(new DataProviderRequest());

        // Assert
        Assert.False(result.IsSuccess);
        Assert.NotNull(result.Error);
    }

    [Fact]
    public async Task FetchAsync_RemovedArticles_AreFiltered()
    {
        // Arrange
        var provider = CreateProvider(HttpStatusCode.OK, RemovedArticlesResponse);

        // Act
        var result = await provider.FetchAsync(new DataProviderRequest());

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Single(result.Value!);
        Assert.Equal("Valid Article", result.Value!.First().Title);
    }

    [Fact]
    public async Task FetchAsync_RelevanceScore_DecreasesWithPosition()
    {
        // Arrange
        var provider = CreateProvider(HttpStatusCode.OK, ValidResponse);

        // Act
        var result = await provider.FetchAsync(new DataProviderRequest());
        var items = result.Value!.ToList();

        // Assert — first item should have higher relevance than second
        Assert.True(items[0].RelevanceScore > items[1].RelevanceScore);
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
    public async Task FetchAsync_HttpRequestException_ReturnsFailure()
    {
        // Arrange
        var handler = new ThrowingHttpMessageHandler(new HttpRequestException("Network error"));
        var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://newsapi.org/v2/")
        };
        var provider = new NewsApiProvider(httpClient, _settingsMock.Object, _loggerMock.Object);

        // Act
        var result = await provider.FetchAsync(new DataProviderRequest());

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Contains("unavailable", result.Error, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task FetchAsync_Timeout_ReturnsFailure()
    {
        // Arrange
        var handler = new ThrowingHttpMessageHandler(new TaskCanceledException("Timeout"));
        var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://newsapi.org/v2/")
        };
        var provider = new NewsApiProvider(httpClient, _settingsMock.Object, _loggerMock.Object);

        // Act
        var result = await provider.FetchAsync(new DataProviderRequest());

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Contains("timed out", result.Error, StringComparison.OrdinalIgnoreCase);
    }
}
