using System.Net;
using System.Text.Json;
using AgileActorsProject.Domain.Common;
using AgileActorsProject.Domain.Entities;
using AgileActorsProject.Domain.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AgileActorsProject.Infrastructure.Providers.GitHub;

public class GitHubProvider : BaseDataProvider
{
    private readonly GitHubSettings _settings;

    public override string ProviderName => "GitHub";

    public GitHubProvider(
        HttpClient httpClient,
        IOptions<GitHubSettings> settings,
        ILogger<GitHubProvider> logger)
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

        if (response.StatusCode == HttpStatusCode.Forbidden)
        {
            Logger.LogWarning("{Provider} rate limit exceeded", ProviderName);
            return Result<IEnumerable<AggregatedItem>>.Failure($"{ProviderName} rate limit exceeded.");
        }

        if (!response.IsSuccessStatusCode)
        {
            Logger.LogWarning("{Provider} returned {StatusCode}", ProviderName, response.StatusCode);
            return Result<IEnumerable<AggregatedItem>>.Failure(
                $"{ProviderName} returned {response.StatusCode}");
        }

        var content = await response.Content.ReadAsStringAsync(cancellationToken);
        var data = JsonSerializer.Deserialize<GitHubSearchResponse>(content);

        if (data is null)
            return Result<IEnumerable<AggregatedItem>>.Failure($"{ProviderName} returned an empty response.");

        if (!data.Items.Any())
            return Result<IEnumerable<AggregatedItem>>.Success(Enumerable.Empty<AggregatedItem>());

        var items = data.Items
            .Select(repo => MapToAggregatedItem(repo, data.TotalCount))
            .ToList();

        return Result<IEnumerable<AggregatedItem>>.Success(items);
    }

    private string BuildUrl(DataProviderRequest request)
    {
        var keyword = !string.IsNullOrWhiteSpace(request.Keyword)
            ? request.Keyword
            : _settings.DefaultQuery;

        var queryParts = new List<string> { keyword };

        if (!string.IsNullOrWhiteSpace(request.Category))
            queryParts.Add($"topic:{request.Category.ToLower()}");

        if (request.From.HasValue)
            queryParts.Add($"pushed:>={request.From.Value:yyyy-MM-dd}");

        var q = Uri.EscapeDataString(string.Join(" ", queryParts));

        return $"search/repositories?q={q}&sort=stars&order=desc&per_page={request.PageSize}";
    }

    private static AggregatedItem MapToAggregatedItem(GitHubRepository repo, int totalCount) => new()
    {
        Title = repo.FullName,
        Summary = repo.Description ?? "No description provided.",
        Url = repo.HtmlUrl,
        Source = "GitHub",
        Category = "Development",
        Timestamp = repo.UpdatedAt,
        RelevanceScore = Math.Log10(repo.StargazersCount + 1) / Math.Log10(totalCount + 1),
        Metadata = new Dictionary<string, object>
    {
        { "stars", repo.StargazersCount },
        { "forks", repo.ForksCount },
        { "language", repo.Language ?? "unknown" },
        { "topics", repo.Topics },
        { "openIssues", repo.OpenIssuesCount },
        { "watchers", repo.WatchersCount }
    }
    };
}
