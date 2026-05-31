using System.Text.Json.Serialization;

namespace AgileActorsProject.Infrastructure.Providers.OpenWeatherMap;

public class OpenWeatherMapResponse
{
    [JsonPropertyName("weather")]
    public List<WeatherDescription> Weather { get; init; } = new();

    [JsonPropertyName("main")]
    public MainData Main { get; init; } = new();

    [JsonPropertyName("wind")]
    public WindData Wind { get; init; } = new();

    [JsonPropertyName("name")]
    public string CityName { get; init; } = string.Empty;

    [JsonPropertyName("dt")]
    public long Timestamp { get; init; }
}

public class WeatherDescription
{
    [JsonPropertyName("main")]
    public string Main { get; init; } = string.Empty;

    [JsonPropertyName("description")]
    public string Description { get; init; } = string.Empty;
}

public class MainData
{
    [JsonPropertyName("temp")]
    public double Temp { get; init; }

    [JsonPropertyName("feels_like")]
    public double FeelsLike { get; init; }

    [JsonPropertyName("humidity")]
    public int Humidity { get; init; }
}

public class WindData
{
    [JsonPropertyName("speed")]
    public double Speed { get; init; }
}
