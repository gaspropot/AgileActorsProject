using AgileActorsProject.Domain.Models;
using AgileActorsProject.Infrastructure.Providers.GitHub;
using AgileActorsProject.Tests.TestHelpers;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using System.Net;

namespace AgileActorsProject.Tests.Providers;

public class GitHubProviderTests
{
    private readonly Mock<ILogger<GitHubProvider>> _loggerMock;
    private readonly Mock<IOptions<GitHubSettings>> _settingsMock;

    private const string ValidResponse = """
        {
            "total_count": 2,
            "items": [
                {
                    "id": 1,
                    "name": "repo-one",
                    "full_name": "owner/repo-one",
                    "description": "First repository",
                    "html_url": "https://github.com/owner/repo-one",
                    "stargazers_count": 5000,
                    "forks_count": 500,
                    "language": "C#",
                    "updated_at": "2024-01-01T10:00:00Z",
                    "topics": ["dotnet", "csharp"],
                    "open_issues_count": 10,
                    "watchers_count": 5000
                },
                {
                    "id": 2,
                    "name": "repo-two",
                    "full_name": "owner/repo-two",
                    "description": "Second repository",
                    "html_url": "https://github.com/owner/repo-two",
                    "stargazers_count": 1000,
                    "forks_count": 100,
                    "language": "Python",
                    "updated_at": "2024-01-01T09:00:00Z",
                    "topics": ["python"],
                    "open_issues_count": 5,
                    "watchers_count": 1000
                }
            ]
        }
        """;

    private const string EmptyItemsResponse = """
        {
            "total_count": 0,
            "items": []
        }
        """;

    private const string NullDescriptionResponse = """
        {
            "total_count": 1,
            "items": [
                {
                    "id": 1,
                    "name": "repo-one",
                    "full_name": "owner/repo-one",
                    "description": null,
                    "html_url": "https://github.com/owner/repo-one",
                    "stargazers_count": 1000,
                    "forks_count": 100,
                    "language": null,
                    "updated_at": "2024-01-01T10:00:00Z",
                    "topics": [],
                    "open_issues_count": 5,
                    "watchers_count": 1000
                }
            ]
        }
        """;

    public GitHubProviderTests()
    {
        _loggerMock = new Mock<ILogger<GitHubProvider>>();
        _settingsMock = new Mock<IOptions<GitHubSettings>>();
        _settingsMock.Setup(s => s.Value).Returns(new GitHubSettings
        {
            ApiKey = "test-api-key",
            DefaultQuery = "stars:>1000",
            BaseUrl = "https://api.github.com/"
        });
    }

    private GitHubProvider CreateProvider(HttpStatusCode statusCode, string responseContent)
    {
        var httpClient = HttpClientFactory.Create(
            statusCode,
            responseContent,
            "https://api.github.com/");

        return new GitHubProvider(httpClient, _settingsMock.Object, _loggerMock.Object);
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
        Assert.Equal("owner/repo-one", item.Title);
        Assert.Equal("First repository", item.Summary);
        Assert.Equal("https://github.com/owner/repo-one", item.Url);
        Assert.Equal("GitHub", item.Source);
        Assert.Equal("Development", item.Category);
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
        Assert.True(item.Metadata.ContainsKey("stars"));
        Assert.True(item.Metadata.ContainsKey("forks"));
        Assert.True(item.Metadata.ContainsKey("language"));
        Assert.True(item.Metadata.ContainsKey("topics"));
        Assert.True(item.Metadata.ContainsKey("openIssues"));
        Assert.True(item.Metadata.ContainsKey("watchers"));
    }

    [Fact]
    public async Task FetchAsync_EmptyItems_ReturnsSuccessWithEmptyCollection()
    {
        // Arrange
        var provider = CreateProvider(HttpStatusCode.OK, EmptyItemsResponse);

        // Act
        var result = await provider.FetchAsync(new DataProviderRequest());

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Empty(result.Value!);
    }

    [Fact]
    public async Task FetchAsync_NullDescription_UsesFallbackSummary()
    {
        // Arrange
        var provider = CreateProvider(HttpStatusCode.OK, NullDescriptionResponse);

        // Act
        var result = await provider.FetchAsync(new DataProviderRequest());
        var item = result.Value!.First();

        // Assert
        Assert.Equal("No description provided.", item.Summary);
    }

    [Fact]
    public async Task FetchAsync_NullLanguage_UsesFallbackInMetadata()
    {
        // Arrange
        var provider = CreateProvider(HttpStatusCode.OK, NullDescriptionResponse);

        // Act
        var result = await provider.FetchAsync(new DataProviderRequest());
        var item = result.Value!.First();

        // Assert
        Assert.Equal("unknown", item.Metadata["language"]);
    }

    [Fact]
    public async Task FetchAsync_RelevanceScore_HigherStarsGivesHigherScore()
    {
        // Arrange
        var provider = CreateProvider(HttpStatusCode.OK, ValidResponse);

        // Act
        var result = await provider.FetchAsync(new DataProviderRequest());
        var items = result.Value!.ToList();

        // Assert — repo with 5000 stars should have higher relevance than 1000 stars
        Assert.True(items[0].RelevanceScore > items[1].RelevanceScore);
    }

    [Fact]
    public async Task FetchAsync_RateLimitExceeded_ReturnsFailure()
    {
        // Arrange
        var provider = CreateProvider(HttpStatusCode.Forbidden, string.Empty);

        // Act
        var result = await provider.FetchAsync(new DataProviderRequest());

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Contains("rate limit", result.Error, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task FetchAsync_NonSuccessStatusCode_ReturnsFailure()
    {
        // Arrange
        var provider = CreateProvider(HttpStatusCode.ServiceUnavailable, string.Empty);

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
            BaseAddress = new Uri("https://api.github.com/")
        };
        var provider = new GitHubProvider(httpClient, _settingsMock.Object, _loggerMock.Object);

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
            BaseAddress = new Uri("https://api.github.com/")
        };
        var provider = new GitHubProvider(httpClient, _settingsMock.Object, _loggerMock.Object);

        // Act
        var result = await provider.FetchAsync(new DataProviderRequest());

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Contains("timed out", result.Error, StringComparison.OrdinalIgnoreCase);
    }
}
