namespace AgileActorsProject.Application.Interfaces;

public interface ICacheService
{
    Task<T?> GetOrSetAsync<T>(string key, Func<Task<T?>> factory, TimeSpan expiration, CancellationToken cancellationToken = default);
}
