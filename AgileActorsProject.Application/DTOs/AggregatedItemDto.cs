namespace AgileActorsProject.Application.DTOs;

public class AggregatedItemDto
{
    public string Id { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public string Summary { get; init; } = string.Empty;
    public string Url { get; init; } = string.Empty;
    public string Source { get; init; } = string.Empty;
    public string Category { get; init; } = string.Empty;
    public DateTime Timestamp { get; init; }
    public double RelevanceScore { get; init; }
    public Dictionary<string, object> Metadata { get; init; } = new();
}
