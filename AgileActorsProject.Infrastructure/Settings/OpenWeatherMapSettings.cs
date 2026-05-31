namespace AgileActorsProject.Infrastructure.Settings;

public class OpenWeatherMapSettings
{
    public string ApiKey { get; init; } = string.Empty;
    public string DefaultCity { get; init; } = "Athens";
    public string BaseUrl { get; init; } = "https://api.openweathermap.org/data/2.5/";
}
