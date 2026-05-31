using AgileActorsProject.Application.DTOs;
using AgileActorsProject.Application.Models;
using AgileActorsProject.Domain.Common;

namespace AgileActorsProject.Application.Interfaces;

public interface IAggregationService
{
    Task<Result<IEnumerable<AggregatedItemDto>>> GetAggregatedDataAsync(AggregationQuery query, CancellationToken cancellationToken = default);
}
