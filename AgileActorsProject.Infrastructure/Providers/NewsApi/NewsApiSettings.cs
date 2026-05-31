namespace AgileActorsProject.Infrastructure.Providers.NewsApi;

public class NewsApiSettings
{
    public string ApiKey { get; init; } = string.Empty;
    public string DefaultSources { get; init; } = "bbc-news,the-verge,reuters";
    public string BaseUrl { get; init; } = "https://newsapi.org/v2/";
}
