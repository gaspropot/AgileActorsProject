using AgileActorsProject.Application.DTOs;

namespace AgileActorsProject.Application.Interfaces;

public interface IStatisticsService
{
    void RecordRequest(string providerName, double responseTimeMs);
    IEnumerable<ProviderStatisticsDto> GetStatistics();
}
