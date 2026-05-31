namespace AgileActorsProject.Application.Interfaces;

public interface IAnomalyDetectionService
{
    Task DetectAndLogAnomaliesAsync(CancellationToken cancellationToken = default);
}
