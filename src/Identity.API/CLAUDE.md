# Identity.API

## Purpose
Authentication and authorization service using ASP.NET Core Identity with OpenID Connect/OAuth2.

## Tech Stack
- ASP.NET Core Identity
- Duende IdentityServer (OpenID Connect)
- PostgreSQL for user storage
- Entity Framework Core

## Key Features
- User registration and login
- OAuth2/OpenID Connect token issuance
- Client application registration
- User profile management

## Clients
Configured for these OAuth clients:
- WebApp
- WebhookClient
- BasketAPI
- OrderingAPI
- WebhooksAPI

## Database
- Uses `identitydb` PostgreSQL database
- Stores users, roles, claims, and tokens

## Key Files
- `Config.cs` - IdentityServer client configuration
- `Data/` - EF Core migrations and contexts
