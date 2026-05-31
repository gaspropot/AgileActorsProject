using System.Diagnostics;
using Microsoft.Extensions.Logging;
using AgileActorsProject.Application.DTOs;
using AgileActorsProject.Application.Interfaces;
using AgileActorsProject.Application.Models;
using AgileActorsProject.Domain.Common;
using AgileActorsProject.Domain.Entities;
using AgileActorsProject.Domain.Interfaces;
using AgileActorsProject.Domain.Models;

namespace AgileActorsProject.Application.Services;

public class AggregationService : IAggregationService
{
    private readonly IEnumerable<IDataProvider> _providers;
    private readonly IStatisticsService _statisticsService;
    private readonly ICacheService _cacheService;
    private readonly ILogger<AggregationService> _logger;

    public AggregationService(
        IEnumerable<IDataProvider> providers,
        IStatisticsService statisticsService,
        ICacheService cacheService,
        ILogger<AggregationService> logger)
    {
        _providers = providers;
        _statisticsService = statisticsService;
        _cacheService = cacheService;
        _logger = logger;
    }

    public async Task<Result<IEnumerable<AggregatedItemDto>>> GetAggregatedDataAsync(
        AggregationQuery query,
        CancellationToken cancellationToken = default)
    {
        var cacheKey = BuildCacheKey(query);

        var cached = await _cacheService.GetOrSetAsync<IEnumerable<AggregatedItemDto>>(
            cacheKey,
            () => FetchAndAggregateAsync(query, cancellationToken),
            TimeSpan.FromMinutes(5),
            cancellationToken);

        if (cached is null)
            return Result<IEnumerable<AggregatedItemDto>>.Failure("Failed to retrieve aggregated data.");

        return Result<IEnumerable<AggregatedItemDto>>.Success(cached);
    }

    private async Task<IEnumerable<AggregatedItemDto>?> FetchAndAggregateAsync(
        AggregationQuery query,
        CancellationToken cancellationToken)
    {
        var request = new DataProviderRequest
        {
            Keyword = query.Keyword,
            Category = query.Category,
            From = query.From,
            To = query.To,
            PageSize = query.PageSize
        };

        var tasks = _providers.Select(provider => FetchFromProviderAsync(provider, request, cancellationToken));
        var results = await Task.WhenAll(tasks);

        var items = results
            .Where(r => r.IsSuccess)
            .SelectMany(r => r.Value!)
            .ToList();

        if (items.Count == 0)
            return null;

        return ApplyFilteringAndSorting(items, query)
            .Select(MapToDto);
    }

    private async Task<Result<IEnumerable<AggregatedItem>>> FetchFromProviderAsync(
        IDataProvider provider,
        DataProviderRequest request,
        CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();
        try
        {
            var result = await provider.FetchAsync(request, cancellationToken);
            stopwatch.Stop();
            _statisticsService.RecordRequest(provider.ProviderName, stopwatch.Elapsed.TotalMilliseconds);
            return result;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _statisticsService.RecordRequest(provider.ProviderName, stopwatch.Elapsed.TotalMilliseconds);
            _logger.LogError(ex, "Unhandled exception from provider {Provider}", provider.ProviderName);
            return Result<IEnumerable<AggregatedItem>>.Failure(ex.Message);
        }
    }

    private static IEnumerable<AggregatedItem> ApplyFilteringAndSorting(
        IEnumerable<AggregatedItem> items,
        AggregationQuery query)
    {
        if (!string.IsNullOrWhiteSpace(query.Category))
            items = items.Where(i => i.Category.Equals(query.Category, StringComparison.OrdinalIgnoreCase));

        if (query.From.HasValue)
            items = items.Where(i => i.Timestamp >= query.From.Value);

        if (query.To.HasValue)
            items = items.Where(i => i.Timestamp <= query.To.Value);

        items = query.SortBy.ToLower() switch
        {
            "relevance" => query.SortOrder == "asc"
                ? items.OrderBy(i => i.RelevanceScore)
                : items.OrderByDescending(i => i.RelevanceScore),
            "source" => query.SortOrder == "asc"
                ? items.OrderBy(i => i.Source)
                : items.OrderByDescending(i => i.Source),
            _ => query.SortOrder == "asc"
                ? items.OrderBy(i => i.Timestamp)
                : items.OrderByDescending(i => i.Timestamp)
        };

        return items;
    }

    private static AggregatedItemDto MapToDto(AggregatedItem item) => new()
    {
        Id = item.Id,
        Title = item.Title,
        Summary = item.Summary,
        Url = item.Url,
        Source = item.Source,
        Category = item.Category,
        Timestamp = item.Timestamp,
        RelevanceScore = item.RelevanceScore,
        Metadata = item.Metadata
    };

    private static string BuildCacheKey(AggregationQuery query) =>
        $"aggregation_{query.Keyword}_{query.Category}_{query.From}_{query.To}_{query.SortBy}_{query.SortOrder}_{query.PageSize}";
}
