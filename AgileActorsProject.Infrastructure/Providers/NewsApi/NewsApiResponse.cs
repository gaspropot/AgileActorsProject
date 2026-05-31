using System.Text.Json.Serialization;

namespace AgileActorsProject.Infrastructure.Providers.NewsApi;

public class NewsApiResponse
{
    [JsonPropertyName("status")]
    public string Status { get; init; } = string.Empty;

    [JsonPropertyName("totalResults")]
    public int TotalResults { get; init; }

    [JsonPropertyName("articles")]
    public List<NewsArticle> Articles { get; init; } = new();
}

public class NewsArticle
{
    [JsonPropertyName("source")]
    public NewsSource Source { get; init; } = new();

    [JsonPropertyName("title")]
    public string Title { get; init; } = string.Empty;

    [JsonPropertyName("description")]
    public string Description { get; init; } = string.Empty;

    [JsonPropertyName("url")]
    public string Url { get; init; } = string.Empty;

    [JsonPropertyName("publishedAt")]
    public DateTime PublishedAt { get; init; }

    [JsonPropertyName("content")]
    public string Content { get; init; } = string.Empty;
}

public class NewsSource
{
    [JsonPropertyName("id")]
    public string? Id { get; init; }

    [JsonPropertyName("name")]
    public string Name { get; init; } = string.Empty;
}
