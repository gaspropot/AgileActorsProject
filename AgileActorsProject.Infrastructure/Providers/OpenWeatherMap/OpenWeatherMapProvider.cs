using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using AgileActorsProject.Domain.Common;
using AgileActorsProject.Domain.Entities;
using AgileActorsProject.Domain.Models;
using AgileActorsProject.Infrastructure.Settings;

namespace AgileActorsProject.Infrastructure.Providers.OpenWeatherMap;

public class OpenWeatherMapProvider : BaseDataProvider
{
    private readonly OpenWeatherMapSettings _settings;

    public override string ProviderName => "OpenWeatherMap";

    public OpenWeatherMapProvider(
        HttpClient httpClient,
        IOptions<OpenWeatherMapSettings> settings,
        ILogger<OpenWeatherMapProvider> logger)
        : base(httpClient, logger)
    {
        _settings = settings.Value;
    }

    protected override async Task<Result<IEnumerable<AggregatedItem>>> FetchInternalAsync(
        DataProviderRequest request,
        CancellationToken cancellationToken)
    {
        var city = request.Keyword ?? _settings.DefaultCity;
        var url = $"weather?q={Uri.EscapeDataString(city)}&appid={_settings.ApiKey}&units=metric";

        var response = await HttpClient.GetAsync(url, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            Logger.LogWarning("{Provider} returned {StatusCode} for city {City}",
                ProviderName, response.StatusCode, city);
            return Result<IEnumerable<AggregatedItem>>.Failure(
                $"{ProviderName} returned {response.StatusCode}");
        }

        var content = await response.Content.ReadAsStringAsync(cancellationToken);
        var data = JsonSerializer.Deserialize<OpenWeatherMapResponse>(content);

        if (data is null)
            return Result<IEnumerable<AggregatedItem>>.Failure($"{ProviderName} returned an empty response.");

        var item = MapToAggregatedItem(data, city);
        return Result<IEnumerable<AggregatedItem>>.Success(new[] { item });
    }

    private static AggregatedItem MapToAggregatedItem(OpenWeatherMapResponse data, string city) => new()
    {
        Title = $"Weather in {data.CityName}",
        Summary = $"{data.Weather.FirstOrDefault()?.Description ?? "N/A"}, " +
                  $"Temp: {data.Main.Temp}°C, " +
                  $"Feels like: {data.Main.FeelsLike}°C, " +
                  $"Humidity: {data.Main.Humidity}%",
        Url = $"https://openweathermap.org/city/{Uri.EscapeDataString(city)}",
        Source = "OpenWeatherMap",
        Category = "Weather",
        Timestamp = DateTimeOffset.FromUnixTimeSeconds(data.Timestamp).UtcDateTime,
        RelevanceScore = 1.0,
        Metadata = new Dictionary<string, object>
    {
        { "temperature", data.Main.Temp },
        { "feelsLike", data.Main.FeelsLike },
        { "humidity", data.Main.Humidity },
        { "windSpeed", data.Wind.Speed },
        { "condition", data.Weather.FirstOrDefault()?.Main ?? "N/A" }
    }
    };
}
