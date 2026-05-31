using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using AgileActorsProject.Application.Interfaces;
using AgileActorsProject.Application.Services;
using AgileActorsProject.Domain.Interfaces;
using AgileActorsProject.Infrastructure.Caching;
using AgileActorsProject.Infrastructure.Providers.GitHub;
using AgileActorsProject.Infrastructure.Providers.NewsApi;
using AgileActorsProject.Infrastructure.Providers.OpenWeatherMap;
using AgileActorsProject.Infrastructure.Services;
using AgileActorsProject.Infrastructure.Settings;
using AgileActorsProject.Infrastructure.Statistics;

namespace AgileActorsProject.WebAPI.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Settings
        services.Configure<OpenWeatherMapSettings>(
            configuration.GetSection(nameof(OpenWeatherMapSettings)));
        services.Configure<NewsApiSettings>(
            configuration.GetSection(nameof(NewsApiSettings)));
        services.Configure<GitHubSettings>(
            configuration.GetSection(nameof(GitHubSettings)));
        services.Configure<AnomalyDetectionSettings>(
            configuration.GetSection(nameof(AnomalyDetectionSettings)));

        // Statistics store — singleton so it lives for the app lifetime
        services.AddSingleton<InMemoryStatisticsStore>();

        // DateTime provider — singleton since it has no state and can be shared across the app
        services.AddSingleton<IDateTimeProvider, DateTimeProvider>();

        // Services
        services.AddScoped<IStatisticsService, StatisticsService>();
        services.AddScoped<IAnomalyDetectionService, AnomalyDetectionService>();
        services.AddScoped<IAggregationService, AggregationService>();

        // Cache
        services.AddMemoryCache();
        services.AddScoped<ICacheService, InMemoryCacheService>();

        // HTTP Clients
        services.AddHttpClient<OpenWeatherMapProvider>(client =>
        {
            var settings = configuration
                .GetSection(nameof(OpenWeatherMapSettings))
                .Get<OpenWeatherMapSettings>()!;
            client.BaseAddress = new Uri(settings.BaseUrl);
            client.Timeout = TimeSpan.FromSeconds(10);
        });

        services.AddHttpClient<NewsApiProvider>(client =>
        {
            var settings = configuration
                .GetSection(nameof(NewsApiSettings))
                .Get<NewsApiSettings>()!;
            client.BaseAddress = new Uri(settings.BaseUrl);
            client.Timeout = TimeSpan.FromSeconds(10);
        });

        services.AddHttpClient<GitHubProvider>(client =>
        {
            var settings = configuration
                .GetSection(nameof(GitHubSettings))
                .Get<GitHubSettings>()!;
            client.BaseAddress = new Uri(settings.BaseUrl);
            client.DefaultRequestHeaders.Add("User-Agent", "AggregatorAPI");
            client.DefaultRequestHeaders.Add("Accept", "application/vnd.github+json");
            if (!string.IsNullOrWhiteSpace(settings.ApiKey))
                client.DefaultRequestHeaders.Add("Authorization", $"Bearer {settings.ApiKey}");
            client.Timeout = TimeSpan.FromSeconds(10);
        });

        // Expose concrete typed client registrations as IDataProvider.
        // This ensures the HttpClient injected into each provider has its BaseAddress and headers correctly configured via the AddHttpClient<T> registrations above.
        services.AddScoped<IDataProvider>(sp => sp.GetRequiredService<OpenWeatherMapProvider>());
        services.AddScoped<IDataProvider>(sp => sp.GetRequiredService<NewsApiProvider>());
        services.AddScoped<IDataProvider>(sp => sp.GetRequiredService<GitHubProvider>());

        return services;
    }

    public static IServiceCollection AddJwtAuthentication(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var jwtSettings = configuration
            .GetSection(nameof(JwtSettings))
            .Get<JwtSettings>()!;

        services.Configure<JwtSettings>(configuration.GetSection(nameof(JwtSettings)));

        // Register the JWT token service
        services.AddScoped<IJwtTokenService, JwtTokenService>();

        services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = jwtSettings.Issuer,
                ValidAudience = jwtSettings.Audience,
                IssuerSigningKey = new SymmetricSecurityKey(
                    Encoding.UTF8.GetBytes(jwtSettings.SecretKey))
            };
        });

        return services;
    }
}
