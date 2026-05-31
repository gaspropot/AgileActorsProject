namespace AgileActorsProject.Domain.Models;

public class DataProviderRequest
{
    public string? Keyword { get; init; }
    public string? Category { get; init; }
    public DateTime? From { get; init; }
    public DateTime? To { get; init; }
    public int PageSize { get; init; } = 20;
}
