# Identity.API

## Overview
The Identity API is the authentication and authorization service for the eShop application. It implements OAuth2 and OpenID Connect protocols using Duende IdentityServer.

## Technology Stack
- **Framework**: ASP.NET Core 10
- **Identity**: ASP.NET Core Identity
- **OAuth2/OIDC**: Duende IdentityServer
- **Database**: PostgreSQL
- **ORM**: Entity Framework Core 10

## Key Components

### IdentityServer Configuration
- OAuth2 and OpenID Connect provider
- JWT token issuance
- Client application registration
- User authentication

### Clients
Pre-configured OAuth2 clients:
- `webapp` - Blazor Server web application
- `webappbff` - Backend-for-frontend
- `basketswaggerui` - Basket API Swagger
- `orderingswaggerui` - Ordering API Swagger
- `webhooksswaggerui` - Webhooks API Swagger
- `maui` - Mobile application client

### Identity Resources
- `openid` - OpenID Connect
- `profile` - User profile information

### API Scopes
- `orders` - Ordering API access
- `basket` - Basket API access
- `webhooks` - Webhooks API access

### User Management
- User registration
- User login/logout
- Password management
- Profile management

## Project Structure
```
Identity.API/
├── Configuration/            # IdentityServer configuration
│   └── Config.cs            # Clients, resources, scopes
├── Data/                    # EF Core context and migrations
│   └── ApplicationDbContext.cs
├── Models/                  # Identity models
│   └── ApplicationUser.cs   # Extended user entity
├── Pages/                   # Razor Pages for auth UI
│   ├── Account/             # Login, logout, register
│   ├── Consent/             # OAuth consent screens
│   ├── Device/              # Device flow
│   └── Grants/              # Grant management
├── Extensions.cs            # Service configuration
└── Program.cs               # Application entry point
```

## Authentication Flows
- **Authorization Code + PKCE** - For web and mobile apps
- **Client Credentials** - For service-to-service
- **Device Flow** - For limited input devices

## Database Schema
Uses ASP.NET Core Identity tables:
- `AspNetUsers` - User accounts
- `AspNetRoles` - Role definitions
- `AspNetUserRoles` - User-role mappings
- `AspNetUserClaims` - User claims
- Plus IdentityServer operational/configuration stores

## Endpoints
- `/.well-known/openid-configuration` - OIDC discovery
- `/connect/authorize` - Authorization endpoint
- `/connect/token` - Token endpoint
- `/connect/userinfo` - User info endpoint
- `/Account/Login` - Login UI
- `/Account/Register` - Registration UI

## Dependencies
- **PostgreSQL**: Identity database
- **Duende IdentityServer**: OAuth2/OIDC implementation
- **ASP.NET Core Identity**: User management

## Configuration
Key settings:
- PostgreSQL connection (via Aspire)
- Client configurations (redirect URIs, secrets)
- Certificate for token signing

## Security Considerations
- HTTPS required in production
- Secure token storage
- PKCE for public clients
- Short-lived access tokens
- Refresh token rotation

## Running Locally
This service is orchestrated by `eShop.AppHost` via .NET Aspire. Run the AppHost project to start all services with proper configuration.
