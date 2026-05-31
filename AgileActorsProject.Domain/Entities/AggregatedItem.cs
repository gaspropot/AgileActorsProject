namespace AgileActorsProject.Domain.Entities;

public class AggregatedItem
{
    public string Id { get; init; } = Guid.NewGuid().ToString();
    public string Title { get; init; } = string.Empty;
    public string Summary { get; init; } = string.Empty;
    public string Url { get; init; } = string.Empty;
    public string Source { get; init; } = string.Empty;       // "OpenWeatherMap", "NewsAPI", "GitHub", potentially others
    public string Category { get; init; } = string.Empty;     // "Weather", "News", "Development" etc
    public DateTime Timestamp { get; init; }
    public double RelevanceScore { get; init; }               // Used for sorting by relevance
    public Dictionary<string, object> Metadata { get; init; } = new(); // Source-specific extras
}
