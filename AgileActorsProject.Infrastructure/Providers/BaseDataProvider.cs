using AgileActorsProject.Domain.Common;
using AgileActorsProject.Domain.Entities;
using AgileActorsProject.Domain.Interfaces;
using AgileActorsProject.Domain.Models;
using Microsoft.Extensions.Logging;

namespace AgileActorsProject.Infrastructure.Providers;

public abstract class BaseDataProvider : IDataProvider
{
    protected readonly HttpClient HttpClient;
    protected readonly ILogger Logger;

    public abstract string ProviderName { get; }

    protected BaseDataProvider(HttpClient httpClient, ILogger logger)
    {
        HttpClient = httpClient;
        Logger = logger;
    }

    public async Task<Result<IEnumerable<AggregatedItem>>> FetchAsync(
        DataProviderRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            return await FetchInternalAsync(request, cancellationToken);
        }
        catch (HttpRequestException ex)
        {
            Logger.LogError(ex, "{Provider} HTTP request failed", ProviderName);
            return Result<IEnumerable<AggregatedItem>>.Failure($"{ProviderName} is currently unavailable.");
        }
        catch (TaskCanceledException ex) when (!cancellationToken.IsCancellationRequested)
        {
            Logger.LogError(ex, "{Provider} request timed out", ProviderName);
            return Result<IEnumerable<AggregatedItem>>.Failure($"{ProviderName} request timed out.");
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "{Provider} unexpected error", ProviderName);
            return Result<IEnumerable<AggregatedItem>>.Failure($"{ProviderName} encountered an unexpected error.");
        }
    }

    protected abstract Task<Result<IEnumerable<AggregatedItem>>> FetchInternalAsync(
        DataProviderRequest request,
        CancellationToken cancellationToken);
}
