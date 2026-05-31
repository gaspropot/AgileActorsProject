namespace AgileActorsProject.Domain.Interfaces;

public interface IDateTimeProvider
{
    DateTime UtcNow { get; }
}
