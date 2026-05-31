using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AgileActorsProject.Application.DTOs;
using AgileActorsProject.Application.Interfaces;

namespace AgileActorsProject.WebAPI.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Authorize]
public class StatisticsController : ControllerBase
{
    private readonly IStatisticsService _statisticsService;

    public StatisticsController(IStatisticsService statisticsService)
    {
        _statisticsService = statisticsService;
    }

    /// <summary>
    /// Retrieves request statistics for all external API providers,
    /// including total requests and response time distribution across performance buckets.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<ProviderStatisticsDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public IActionResult GetStatistics()
    {
        var statistics = _statisticsService.GetStatistics();
        return Ok(statistics);
    }
}
