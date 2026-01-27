# Memory: tdd-red - todays-weather-endpoint

## Current State
Status: Complete
Last Updated: 2025-12-19

## Context & Knowledge
*   Feature: Today's Weather Endpoint.
*   Solution C: Configurable mock/real provider toggle.
*   Tech Stack: .NET 8 Minimal API.
*   Existing Repository: `IWeatherRepository`, `MockWeatherRepository`.
*   New Requirements: Endpoint contract, toggle behavior, caching, error handling/resilience.
*   Created `CurrentWeatherResponse` model and `IWeatherProvider` interface to support tests.
*   Created stubs for `MockWeatherProvider`, `WeatherApiProvider`, and `CachedWeatherProvider`.

## Decisions Log
*   [Decision 1]: Use `tdd-red` style, writing tests that fail initially.
*   [Decision 2]: Organize tests under `WeatherForecastApi.Tests/Integration/` and `WeatherForecastApi.Tests/Repositories/`.
*   [Decision 3]: Renamed new response model to `CurrentWeatherResponse` to avoid conflict with existing `WeatherForecastResponse`.

## Work in Progress
*   Tests implemented and verified to fail.
    1. Endpoint contract (request validation).
    2. Toggle behavior (mock vs real).
    3. Caching behavior.
    4. Error handling/resilience.

## Next Steps
*   Hand over to `tdd-green` to implement the logic and make tests pass.
