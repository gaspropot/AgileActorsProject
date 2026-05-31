namespace AgileActorsProject.Infrastructure.Settings;

public class AnomalyDetectionSettings
{
    public int IntervalSeconds { get; init; } = 30;
    public double AnomalyThresholdPercent { get; init; } = 50.0;
}
