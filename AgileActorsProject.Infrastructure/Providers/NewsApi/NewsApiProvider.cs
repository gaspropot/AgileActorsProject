using System.Text.Json;
using AgileActorsProject.Domain.Common;
using AgileActorsProject.Domain.Entities;
using AgileActorsProject.Domain.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AgileActorsProject.Infrastructure.Providers.NewsApi;

public class NewsApiProvider : BaseDataProvider
{
    private readonly NewsApiSettings _settings;

    public override string ProviderName => "NewsAPI";

    public NewsApiProvider(
        HttpClient httpClient,
        IOptions<NewsApiSettings> settings,
        ILogger<NewsApiProvider> logger)
        : base(httpClient, logger)
    {
        _settings = settings.Value;
    }

    protected override async Task<Result<IEnumerable<AggregatedItem>>> FetchInternalAsync(
        DataProviderRequest request,
        CancellationToken cancellationToken)
    {
        var url = BuildUrl(request);

        var response = await HttpClient.GetAsync(url, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            Logger.LogWarning("{Provider} returned {StatusCode}", ProviderName, response.StatusCode);
            return Result<IEnumerable<AggregatedItem>>.Failure(
                $"{ProviderName} returned {response.StatusCode}");
        }

        var content = await response.Content.ReadAsStringAsync(cancellationToken);
        var data = JsonSerializer.Deserialize<NewsApiResponse>(content);

        if (data is null || data.Status != "ok")
            return Result<IEnumerable<AggregatedItem>>.Failure($"{ProviderName} returned an invalid response.");

        if (!data.Articles.Any())
            return Result<IEnumerable<AggregatedItem>>.Success(Enumerable.Empty<AggregatedItem>());

        var items = data.Articles
            .Where(a => !string.IsNullOrWhiteSpace(a.Title) && a.Title != "[Removed]")
            .Select((article, index) => MapToAggregatedItem(article, index, data.TotalResults))
            .ToList();

        return Result<IEnumerable<AggregatedItem>>.Success(items);
    }

    private string BuildUrl(DataProviderRequest request)
    {
        var queryParams = new List<string>
    {
        $"apiKey={_settings.ApiKey}",
        $"pageSize={request.PageSize}"
    };

        if (!string.IsNullOrWhiteSpace(request.Keyword))
            queryParams.Add($"q={Uri.EscapeDataString(request.Keyword)}");

        if (!string.IsNullOrWhiteSpace(request.Category) && IsValidCategory(request.Category))
            queryParams.Add($"category={request.Category.ToLower()}");

        if (request.From.HasValue)
            queryParams.Add($"from={request.From.Value:yyyy-MM-dd}");

        if (request.To.HasValue)
            queryParams.Add($"to={request.To.Value:yyyy-MM-dd}");

        // NewsAPI requires either q or category for everything endpoint
        // fall back to top-headlines with default category if neither provided
        var endpoint = !string.IsNullOrWhiteSpace(request.Keyword)
            ? "everything"
            : "top-headlines";

        if (endpoint == "top-headlines" && !queryParams.Any(p => p.StartsWith("category")))
            queryParams.Add($"sources={_settings.DefaultSources}");

        return $"{endpoint}?{string.Join("&", queryParams)}";
    }

    private static bool IsValidCategory(string category) =>
        new[] { "business", "entertainment", "general", "health", "science", "sports", "technology" }
        .Contains(category.ToLower());

    private static AggregatedItem MapToAggregatedItem(NewsArticle article, int index, int totalResults) => new()
    {
        Title = article.Title,
        Summary = article.Description ?? string.Empty,
        Url = article.Url,
        Source = "NewsAPI",
        Category = "News",
        Timestamp = article.PublishedAt,
        RelevanceScore = 1.0 - (double)index / totalResults,
        Metadata = new Dictionary<string, object>
    {
        { "sourceName", article.Source.Name },
        { "sourceId", article.Source.Id ?? "unknown" },
        { "content", article.Content ?? string.Empty }
    }
    };
}
