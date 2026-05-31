# Aggregator API - Agile Actors Project

A .NET 10 API aggregation service that consolidates data from multiple external APIs and delivers it through a single unified endpoint.

## Overview

The service fetches data simultaneously from three external APIs:
- **OpenWeatherMap** — current weather data by city
- **NewsAPI** — latest news articles by keyword or category
- **GitHub** — repositories by keyword or topic

Results are normalized into a common shape, merged into a single feed, and exposed through a RESTful API with filtering, sorting, and JWT authentication.

## Architecture

The solution follows **Clean Architecture** with four separate projects:

```
AgileActorsProject/
├── src/
│   ├── AgileActorsProject.Domain          # Core entities, Result<T>, interfaces
│   ├── AgileActorsProject.Application     # Use cases, DTOs, service interfaces
│   ├── AgileActorsProject.Infrastructure  # External API clients, caching, statistics
│   └── AgileActorsProject.API             # Controllers, JWT, background service
│   └── AgileActorsProject.Tests           # xUnit unit tests
│

```

### Key Design Decisions

- **Result\<T\> pattern** — used at the provider and aggregation service level to model expected failures (provider unavailable, timeout) without exceptions
- **IDataProvider interface** — every external API client implements this interface. Adding a new source requires only a new class and a DI registration, nothing else changes
- **Parallel fetching** — all providers are called simultaneously via `Task.WhenAll`, minimizing response time
- **Graceful degradation** — a provider failure is isolated; the aggregation continues with results from the remaining providers
- **IDateTimeProvider abstraction** — time is injected rather than called directly, making time-sensitive logic fully testable

## Prerequisites

- .NET 10 SDK
- API keys for:
  - [OpenWeatherMap](https://openweathermap.org/api)
  - [NewsAPI](https://newsapi.org)
  - [GitHub](https://github.com/settings/tokens)

## Configuration

Add your API keys to `appsettings.json` in `AgileActorsProject.WebApi`:

```json
{
  "JwtSettings": {
    "SecretKey": "your-super-secret-key-that-is-long-enough",
    "Issuer": "AggregatorAPI",
    "Audience": "AggregatorAPIUsers",
    "ExpirationMinutes": 60
  },
  "OpenWeatherMapSettings": {
    "ApiKey": "your-openweathermap-api-key",
    "DefaultCity": "Athens",
    "BaseUrl": "https://api.openweathermap.org/data/2.5/"
  },
  "NewsApiSettings": {
    "ApiKey": "your-newsapi-key",
    "DefaultSources": "bbc-news,the-verge,reuters",
    "BaseUrl": "https://newsapi.org/v2/"
  },
  "GitHubSettings": {
    "ApiKey": "your-github-token",
    "DefaultQuery": "stars:>1000",
    "BaseUrl": "https://api.github.com/"
  },
  "CacheSettings": {
    "ExpirationMinutes": 5
  },
  "AnomalyDetectionSettings": {
    "IntervalSeconds": 30,
    "AnomalyThresholdPercent": 50.0
  }
}
```

## Running the Application

```bash
cd AgileActorsProject.Tests
dotnet run
```

The API will be available at `https://localhost:5248`.
The Scalar API reference UI will be available at `https://localhost:5248/scalar/v1` in Development.

## Running the Tests

```bash
cd AgileActorsProject.Tests
dotnet test
```

## Authentication

All endpoints except `/api/v1/auth/token` require a valid JWT bearer token.

**Step 1 — Obtain a token:**

```http
POST /api/v1/auth/token
Content-Type: application/json

{
  "username": "admin",
  "password": "admin"
}
```

**Response:**
```json
{
  "token": "eyJhbGciOiJIUzI1NiIs..."
}
```

**Step 2 — Include the token in subsequent requests:**

```http
Authorization: Bearer eyJhbGciOiJIUzI1NiIs...
```

## API Endpoints

### GET /api/v1/aggregation

Retrieves aggregated data from all configured external APIs.

**Query Parameters:**

| Parameter | Type | Default | Description |
|---|---|---|---|
| keyword | string | null | Search term passed to all providers |
| category | string | null | Filter by category (Weather, News, Development) |
| from | DateTime | null | Filter items from this date (UTC) |
| to | DateTime | null | Filter items up to this date (UTC) |
| sortBy | string | timestamp | Sort field: `timestamp`, `relevance`, `source` |
| sortOrder | string | desc | Sort direction: `asc`, `desc` |
| pageSize | int | 20 | Number of items per provider (1-100) |

**Example Request:**
```http
GET /api/v1/aggregation?keyword=dotnet&sortBy=relevance&sortOrder=desc&pageSize=10
Authorization: Bearer eyJhbGciOiJIUzI1NiIs...
```

**Example Response:**
```json
[
  {
    "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "title": "owner/awesome-dotnet",
    "summary": "A curated list of awesome .NET libraries",
    "url": "https://github.com/owner/awesome-dotnet",
    "source": "GitHub",
    "category": "Development",
    "timestamp": "2024-01-01T10:00:00Z",
    "relevanceScore": 0.95,
    "metadata": {
      "stars": 15000,
      "forks": 2000,
      "language": "C#",
      "topics": ["dotnet", "csharp"],
      "openIssues": 50,
      "watchers": 15000
    }
  }
]
```

**Response Codes:**

| Code | Description |
|---|---|
| 200 | Success |
| 400 | Invalid query parameters |
| 401 | Missing or invalid JWT token |
| 500 | All providers failed |

---

### GET /api/v1/statistics

Retrieves request statistics for all external API providers.

**Example Request:**
```http
GET /api/v1/statistics
Authorization: Bearer eyJhbGciOiJIUzI1NiIs...
```

**Example Response:**
```json
[
  {
    "providerName": "GitHub",
    "totalRequests": 42,
    "averageResponseTimeMs": 850.5,
    "buckets": {
      "fast": 10,
      "average": 25,
      "slow": 7
    }
  }
]
```

**Performance Buckets:**

| Bucket | Threshold |
|---|---|
| Fast | < 500ms |
| Average | 500ms — 1500ms |
| Slow | > 1500ms |

**Response Codes:**

| Code | Description |
|---|---|
| 200 | Success |
| 401 | Missing or invalid JWT token |

---

### POST /api/v1/auth/token

Generates a JWT bearer token for API access.

**Request Body:**
```json
{
  "username": "admin",
  "password": "admin"
}
```

**Response Codes:**

| Code | Description |
|---|---|
| 200 | Token generated successfully |
| 400 | Invalid credentials |

## Caching

Aggregated responses are cached in memory for 5 minutes (configurable via `CacheSettings:ExpirationMinutes`). The cache key is derived from all query parameters, so different queries are cached independently. Caching is bypassed on cache miss and populated on the first successful aggregation.
In a real application, this cache would probably be in Redis, not IMemoryCache.

## Background Anomaly Detection

A background service runs every 30 seconds (configurable via `AnomalyDetectionSettings:IntervalSeconds`) and compares each provider's average response time over the last 5 minutes against its overall average. If the recent average exceeds the overall average by more than 50% (configurable via `AnomalyDetectionSettings:AnomalyThresholdPercent`), a warning is logged:

```
warn: Performance anomaly detected for GitHub:
      recent average 2500.00ms is 75.3% above overall average 1428.00ms
```

## Error Handling

| Scenario | Behavior |
|---|---|
| Single provider unavailable | Aggregation continues with remaining providers |
| All providers unavailable | Returns 500 with error message |
| Provider timeout | Treated as failure, logged, excluded from results |
| Invalid query parameters | Returns 400 immediately without calling providers |
| Unhandled provider exception | Caught, logged, treated as failure |

## Adding a New Provider

1. Create a new class in `AgileActorsProject.Infrastructure/Providers/` implementing `IDataProvider`
2. Extend `BaseDataProvider` to inherit error handling
3. Add settings class in `AgileActorsProject.Infrastructure/Settings/`
4. Register the HTTP client and provider in `ServiceCollectionExtensions`
5. Add configuration to `appsettings.json`

No other changes required — the aggregation service discovers all `IDataProvider` registrations automatically via `IEnumerable<IDataProvider>` injection.
