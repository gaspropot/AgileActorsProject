using AgileActorsProject.Domain.Common;
using AgileActorsProject.Domain.Entities;
using AgileActorsProject.Domain.Models;

namespace AgileActorsProject.Domain.Interfaces;

public interface IDataProvider
{
    string ProviderName { get; }

    Task<Result<IEnumerable<AggregatedItem>>> FetchAsync(DataProviderRequest request, CancellationToken cancellationToken = default);
}
