using System.Net;

namespace AgileActorsProject.Tests.TestHelpers;

public static class HttpClientFactory
{
    public static HttpClient Create(HttpStatusCode statusCode, string responseContent, string baseUrl)
    {
        var handler = new MockHttpMessageHandler(statusCode, responseContent);
        return new HttpClient(handler)
        {
            BaseAddress = new Uri(baseUrl)
        };
    }
}
