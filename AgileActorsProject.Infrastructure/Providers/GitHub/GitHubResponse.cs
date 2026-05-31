using System.Text.Json.Serialization;

namespace AgileActorsProject.Infrastructure.Providers.GitHub;

public class GitHubSearchResponse
{
    [JsonPropertyName("total_count")]
    public int TotalCount { get; init; }

    [JsonPropertyName("items")]
    public List<GitHubRepository> Items { get; init; } = new();
}

public class GitHubRepository
{
    [JsonPropertyName("id")]
    public long Id { get; init; }

    [JsonPropertyName("name")]
    public string Name { get; init; } = string.Empty;

    [JsonPropertyName("full_name")]
    public string FullName { get; init; } = string.Empty;

    [JsonPropertyName("description")]
    public string? Description { get; init; }

    [JsonPropertyName("html_url")]
    public string HtmlUrl { get; init; } = string.Empty;

    [JsonPropertyName("stargazers_count")]
    public int StargazersCount { get; init; }

    [JsonPropertyName("forks_count")]
    public int ForksCount { get; init; }

    [JsonPropertyName("language")]
    public string? Language { get; init; }

    [JsonPropertyName("updated_at")]
    public DateTime UpdatedAt { get; init; }

    [JsonPropertyName("topics")]
    public List<string> Topics { get; init; } = new();

    [JsonPropertyName("open_issues_count")]
    public int OpenIssuesCount { get; init; }

    [JsonPropertyName("watchers_count")]
    public int WatchersCount { get; init; }
}
