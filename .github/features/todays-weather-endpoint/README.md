# Feature: Today's Weather Endpoint

## Status
- **Phase:** Research Complete
- **Last Updated:** 2025-12-19
- **Current Agent:** Domain Specialist (completed)
- **Next Agent:** Implementation Agent or Test Writer

## Overview
Build an API endpoint that returns today's current weather conditions for a requested location.

## Requirements
- Support multiple location formats (city name, lat/long, postal code)
- Return comprehensive weather data (temperature, conditions, humidity, wind, precipitation, etc.)
- Implement caching and rate limiting
- Robust error handling
- Testable with mocked data
- Security and operational readiness

## Research Findings

### Recommended Solution
**Solution B: Repository Pattern with Provider Abstraction**

- **Tech Stack:** .NET 8 Minimal API (existing)
- **Provider:** WeatherAPI.com (free tier, 1M calls/month)
- **Architecture:** Repository pattern with provider abstraction, decorator for caching
- **Caching:** In-memory, 10-minute TTL
- **Rate Limiting:** AspNetCoreRateLimit (60 req/min per IP)
- **Resilience:** Polly (retry, circuit breaker, timeout)
- **Effort:** 12-16 hours / 2-3 days

### Key Components
1. Extend `IWeatherRepository` interface
2. Create `IExternalWeatherProvider` abstraction
3. Implement `WeatherApiProvider` (calls external API)
4. Implement `WeatherProviderRepository` (business logic)
5. Implement `CachedWeatherRepository` decorator
6. Create Minimal API endpoint: `GET /api/weather/today?location={location}`
7. Add Polly resilience policies
8. Add rate limiting middleware
9. Comprehensive tests (unit + integration)

### Endpoint Design
```
GET /api/weather/today?location=London,UK
GET /api/weather/today?location=51.5074,-0.1278
GET /api/weather/today?location=90210
```

### Response Example
```json
{
  "location": {
    "name": "London",
    "country": "United Kingdom",
    "latitude": 51.5074,
    "longitude": -0.1278
  },
  "current": {
    "observationTime": "2025-12-19T14:00:00Z",
    "temperature": {
      "celsius": 12.5,
      "fahrenheit": 54.5
    },
    "condition": {
      "text": "Partly cloudy"
    },
    "humidity": 72,
    "wind": {
      "speedKph": 15.5,
      "direction": "SW"
    },
    "precipitation": {
      "mm": 0.0,
      "chance": 10
    }
  }
}
```

## Files
- `findings.domain_specialist.md` - Comprehensive research findings
- `todays-weather-endpoint.domain_specialist.memory.md` - Agent memory and context
- `README.md` - This file (feature status and summary)

## Next Steps
1. Obtain WeatherAPI.com API key
2. Configure User Secrets for API key
3. Implement Solution B following findings document
4. Write tests (TDD approach)
5. Update Swagger documentation
6. Deploy to staging and test
7. Set up monitoring and logging
8. Deploy to production

## Trade-Offs Considered

### Why Not Solution A (Simple Pass-Through)?
- Lacks provider abstraction (harder to swap providers)
- No resilience patterns
- Less testable

### Why Not Solution C (Event-Driven Background Refresh)?
- Over-engineered for this use case
- Can't pre-cache infinite location space
- Unnecessary complexity

## Dependencies
- WeatherAPI.com API key (free tier)
- NuGet packages to add:
  - `Polly` (resilience)
  - `Polly.Extensions.Http`
  - `AspNetCoreRateLimit` (rate limiting)

## Risks & Mitigations
- **Risk:** External API downtime → **Mitigation:** Circuit breaker, cache stale data
- **Risk:** Rate limit exceeded → **Mitigation:** Client-side rate limiting, caching
- **Risk:** API key exposure → **Mitigation:** User Secrets, Key Vault, never commit
- **Risk:** Invalid locations → **Mitigation:** Robust parsing and validation

## Success Criteria
- [x] Research complete with findings documented
- [ ] Implementation complete with all components
- [ ] Tests passing (80%+ coverage)
- [ ] Swagger documentation updated
- [ ] Deployed to staging
- [ ] Monitoring and logging configured
- [ ] Deployed to production
