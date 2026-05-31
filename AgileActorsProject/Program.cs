using AgileActorsProject.WebAPI.BackgroundServices;
using AgileActorsProject.WebAPI.Extensions;
using Microsoft.OpenApi;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// Controllers
builder.Services.AddControllers();

// Scalar
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApi(options =>
{
    options.AddDocumentTransformer((document, context, ct) =>
    {
        document.Info = new OpenApiInfo
        {
            Title = "Aggregator API",
            Version = "v1",
            Description = "An API aggregation service consolidating data from OpenWeatherMap, NewsAPI, and GitHub."
        };

        document.Components ??= new OpenApiComponents();
        document.Components.SecuritySchemes ??= new Dictionary<string, IOpenApiSecurityScheme>();
        document.Components.SecuritySchemes.Add("Bearer", new OpenApiSecurityScheme
        {
            Type = SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT",
            Description = "Enter your JWT token below."
        });

        return Task.CompletedTask;
    });
});

// Infrastructure + JWT
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddJwtAuthentication(builder.Configuration);

// Background service
builder.Services.AddHostedService<AnomalyDetectionBackgroundService>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.UseHttpsRedirection();
// Authentication ALWAYS before authorization
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();