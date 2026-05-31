namespace AgileActorsProject.Application.Models;

public class AggregationQuery
{
    public string? Keyword { get; init; }
    public string? Category { get; init; }
    public DateTime? From { get; init; }
    public DateTime? To { get; init; }
    public string SortBy { get; init; } = "timestamp";     // "timestamp", "relevance", "source"
    public string SortOrder { get; init; } = "desc";       // "asc", "desc"
    public int PageSize { get; init; } = 20;
}
