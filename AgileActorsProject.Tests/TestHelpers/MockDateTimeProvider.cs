using AgileActorsProject.Domain.Interfaces;

namespace AgileActorsProject.Tests.TestHelpers;

public class MockDateTimeProvider : IDateTimeProvider
{
    private DateTime _utcNow;

    public MockDateTimeProvider(DateTime utcNow)
    {
        _utcNow = utcNow;
    }

    public DateTime UtcNow => _utcNow;

    public void Advance(TimeSpan timeSpan)
    {
        _utcNow += timeSpan;
    }
}
