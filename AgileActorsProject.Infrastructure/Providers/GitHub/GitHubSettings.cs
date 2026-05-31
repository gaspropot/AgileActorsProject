namespace AgileActorsProject.Infrastructure.Providers.GitHub;

public class GitHubSettings
{
    public string ApiKey { get; init; } = string.Empty;
    public string DefaultQuery { get; init; } = "stars:>1000";
    public string BaseUrl { get; init; } = "https://api.github.com/";
}
