# Feature: Today's Weather Endpoint - Development Plan

## Context
Based on findings in: [findings.domain_specialist.md](findings.domain_specialist.md)
This plan implements **Solution C: Configurable mock/real provider toggle**, allowing the API to switch between a mock data source and a real external weather provider (WeatherAPI.com) via configuration.

## Plan

### Phase 1: Configuration & Infrastructure
- [ ] **Step 1**: Define configuration schema and update settings.
  - **Agent**: tdd-green
  - **Details**: Create `WeatherSettings` class in `Models/Configuration.cs`. Update `appsettings.json` and `appsettings.Development.json` with `WeatherSettings` section including `ProviderType` ("Mock" or "Real"), `ApiKey`, and `BaseUrl`.
- [ ] **Step 2**: Define the Provider Abstraction.
  - **Agent**: tdd-green
  - **Details**: Create `IWeatherProvider` interface in `Repositories/` with `Task<WeatherForecastResponse> GetCurrentWeatherAsync(string location)`.

### Phase 2: Mock Provider Implementation
- [ ] **Step 3**: Write tests for MockWeatherProvider.
  - **Agent**: tdd-red
  - **Details**: Create `MockWeatherProviderTests.cs` in `WeatherForecastApi.Tests/Repositories/`. Test that it returns deterministic data for known locations.
- [ ] **Step 4**: Implement MockWeatherProvider.
  - **Agent**: tdd-green
  - **Details**: Implement `MockWeatherProvider` in `Repositories/`. It should return hardcoded or generated data similar to the existing `MockWeatherRepository`.

### Phase 3: Real Provider Implementation (WeatherAPI.com)
- [ ] **Step 5**: Write tests for WeatherApiProvider.
  - **Agent**: tdd-red
  - **Details**: Create `WeatherApiProviderTests.cs`. Use `MockHttpMessageHandler` to simulate successful and failed responses from WeatherAPI.com.
- [ ] **Step 6**: Implement WeatherApiProvider.
  - **Agent**: tdd-green
  - **Details**: Implement `WeatherApiProvider` in `Repositories/`. Use `HttpClient` to call WeatherAPI.com and map the response to `WeatherForecastResponse`.

### Phase 4: Provider Toggle & Dependency Injection
- [ ] **Step 7**: Write tests for Provider Factory/Toggle.
  - **Agent**: tdd-red
  - **Details**: Create tests to verify that `IWeatherProvider` resolves to `MockWeatherProvider` when configured as "Mock" and `WeatherApiProvider` when "Real".
- [ ] **Step 8**: Implement Provider Toggle in DI.
  - **Agent**: tdd-green
  - **Details**: Update `Program.cs` to register the appropriate `IWeatherProvider` implementation based on `WeatherSettings:ProviderType`.

### Phase 5: Caching & Resilience
- [ ] **Step 9**: Write tests for CachedWeatherProvider (Decorator).
  - **Agent**: tdd-red
  - **Details**: Create `CachedWeatherProviderTests.cs`. Verify that it calls the inner provider on cache miss and returns cached data on cache hit.
- [ ] **Step 10**: Implement CachedWeatherProvider.
  - **Agent**: tdd-green
  - **Details**: Implement `CachedWeatherProvider` as a decorator for `IWeatherProvider` using `IMemoryCache`. Register it in `Program.cs` using the decorator pattern (e.g., Scrutor or manual registration).
- [ ] **Step 11**: Add Resilience Policies (Polly).
  - **Agent**: tdd-green
  - **Details**: Configure `HttpClient` for `WeatherApiProvider` with Polly policies: Retry (3 times with exponential backoff) and Circuit Breaker.

### Phase 6: API Endpoint & Integration
- [ ] **Step 12**: Write Integration Tests for the endpoint.
  - **Agent**: tdd-red
  - **Details**: Create `TodayWeatherEndpointTests.cs` in `WeatherForecastApi.Tests/Integration/`. Test `GET /api/weather/today?location={location}` for success, 404 (not found), and 400 (missing location).
- [ ] **Step 13**: Implement the Today's Weather Endpoint.
  - **Agent**: tdd-green
  - **Details**: Add the endpoint to `Program.cs` using Minimal API syntax. It should inject `IWeatherProvider` and return the weather data.
- [ ] **Step 14**: Add Rate Limiting.
  - **Agent**: tdd-green
  - **Details**: Configure `AspNetCoreRateLimit` or .NET 8 built-in rate limiting for the new endpoint.

### Phase 7: Documentation & Finalization
- [ ] **Step 15**: Update Swagger and Documentation.
  - **Agent**: tdd-green
  - **Details**: Ensure the endpoint is properly documented in Swagger (descriptions, example values). Update `README.md` with instructions on how to toggle between Mock and Real providers.
