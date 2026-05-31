using AgileActorsProject.Domain.Interfaces;

namespace AgileActorsProject.Infrastructure.Services;

//TLDR: I created this purely for testability reasons, because AnomalyDetectionService uses a 5-minute window
public class DateTimeProvider : IDateTimeProvider
{
    public DateTime UtcNow => DateTime.UtcNow;
}
