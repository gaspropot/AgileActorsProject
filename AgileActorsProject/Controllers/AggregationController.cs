using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AgileActorsProject.Application.DTOs;
using AgileActorsProject.Application.Interfaces;
using AgileActorsProject.Application.Models;

namespace AgileActorsProject.WebAPI.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Authorize]
public class AggregationController : ControllerBase
{
    private readonly IAggregationService _aggregationService;

    public AggregationController(IAggregationService aggregationService)
    {
        _aggregationService = aggregationService;
    }

    /// <summary>
    /// Retrieves aggregated data from all configured external APIs.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<AggregatedItemDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetAggregatedData(
        [FromQuery] string? keyword,
        [FromQuery] string? category,
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        [FromQuery] string sortBy = "timestamp",
        [FromQuery] string sortOrder = "desc",
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        if (pageSize is < 1 or > 100)
            return BadRequest("pageSize must be between 1 and 100.");

        if (!new[] { "timestamp", "relevance", "source" }.Contains(sortBy.ToLower()))
            return BadRequest("sortBy must be one of: timestamp, relevance, source.");

        if (!new[] { "asc", "desc" }.Contains(sortOrder.ToLower()))
            return BadRequest("sortOrder must be one of: asc, desc.");

        if (from.HasValue && to.HasValue && from > to)
            return BadRequest("'from' must be earlier than 'to'.");

        var query = new AggregationQuery
        {
            Keyword = keyword,
            Category = category,
            From = from,
            To = to,
            SortBy = sortBy,
            SortOrder = sortOrder,
            PageSize = pageSize
        };

        var result = await _aggregationService.GetAggregatedDataAsync(query, cancellationToken);

        return result.IsSuccess
            ? Ok(result.Value)
            : StatusCode(StatusCodes.Status500InternalServerError, new { error = result.Error });
    }
}
