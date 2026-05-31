namespace AgileActorsProject.Application.DTOs;

public class ProviderStatisticsDto
{
    public string ProviderName { get; init; } = string.Empty;
    public int TotalRequests { get; init; }
    public double AverageResponseTimeMs { get; init; }
    public PerformanceBucketsDto Buckets { get; init; } = new();
}

public class PerformanceBucketsDto
{
    public int Fast { get; init; }      // < 500ms
    public int Average { get; init; }   // 500-1500ms
    public int Slow { get; init; }      // > 1500ms
}
