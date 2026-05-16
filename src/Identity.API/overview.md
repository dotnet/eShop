# Identity.API — Centralized OAuth 2.0 / OpenID Connect Identity Provider

## Summary

- Provides centralized authentication and authorization for all eShop microservices using Duende IdentityServer 7
- Supports multiple OAuth 2.0 client configurations (MAUI mobile, Web app, Webhooks, Swagger UIs) with OpenID Connect flows
- Stores user identity and payment/shipping profile data in PostgreSQL via ASP.NET Identity and Entity Framework Core
- Issues JWT bearer tokens with claims to protect downstream APIs (Ordering, Basket, Webhooks)
- Enforces user scope-based consent and token lifetime policies

---

## Projects and Folder Map

PATH: `src/Identity.API`
PURPOSE: OAuth 2.0 / OIDC authentication server providing centralized identity management for the eShop platform
ENTRY_FILES:
  - `Program.cs` — ASP.NET Core application bootstrap, IdentityServer configuration, service registration
  - `Configuration/Config.cs` — In-memory IdentityServer clients, API scopes, and identity resources
  - `Models/ApplicationUser.cs` — Extended identity user with payment and shipping profile fields
  - `Data/ApplicationDbContext.cs` — Entity Framework Core DbContext for ASP.NET Identity persistence
  - `UsersSeed.cs` — Database seeding with default test user (alice)

---

## Components

COMPONENT_NAME: IdentityServer
TYPE: Service
PURPOSE: Central OpenID Connect and OAuth 2.0 server that authenticates users and issues security tokens.
RESPONSIBILITIES:
  - Configures and enforces OAuth 2.0 client definitions (6 clients: MAUI, WebApp, Webhooks, 3x Swagger)
  - Issues JWT access tokens and ID tokens with claims for authenticated users
  - Validates redirect URIs and enforces PKCE for native clients
  - Implements token lifetime policies and cookie-based session management
  - Revokes consent and manages user authentication context
SOURCE: `Program.cs` (lines 15–41)
CALLS:
  - ProfileService — to populate token claims from ApplicationUser
  - EFLoginService — to validate credentials during authentication
  - ApplicationDbContext — to verify user active status and security stamp
CALLED_BY:
  - AccountController — through IIdentityServerInteractionService
  - ConsentController — through IIdentityServerInteractionService
  - Downstream OAuth 2.0 clients (MAUI, WebApp, external systems)

---

COMPONENT_NAME: AccountController
TYPE: API
PURPOSE: Handles user login and logout workflows through the browser UI.
RESPONSIBILITIES:
  - Renders the login form for GET /Account/Login
  - Processes credential submissions and delegates to EFLoginService for validation
  - Verifies authorization context and enforces authentication requirements
  - Issues post-login redirect responses based on the originating client
  - Handles logout and user sign-out operations
SOURCE: `Quickstart/Account/AccountController.cs`
CALLS:
  - EFLoginService — to find user by username and validate password
  - IIdentityServerInteractionService — to retrieve and manage authorization context
  - SignInManager — to establish authenticated session after credential validation
CALLED_BY:
  - Web browsers (user agents) issuing HTTP requests

---

COMPONENT_NAME: ConsentController
TYPE: API
PURPOSE: Handles user scope-consent workflows for third-party client applications.
RESPONSIBILITIES:
  - Displays scope permissions that the user must approve before granting access
  - Processes POST requests to accept or deny requested API scopes
  - Persists user consent decisions in IdentityServer's consent store
  - Returns authorization approval or denial via IIdentityServerInteractionService
SOURCE: `Quickstart/Consent/ConsentController.cs`
CALLS:
  - IIdentityServerInteractionService — to retrieve authorization request context and record consent decision
  - IEventService — to log consent acceptance or denial
CALLED_BY:
  - Web browsers during Authorization Code flow for clients that require consent

---

COMPONENT_NAME: ProfileService
TYPE: Service
PURPOSE: Populates JWT token claims from the ApplicationUser store.
RESPONSIBILITIES:
  - Retrieves claims for the authenticated subject (user ID)
  - Returns claims to be embedded in ID tokens and access tokens
  - Validates user's active status (not locked out, not deleted)
  - Checks security stamp to detect password changes or account invalidation
  - Maps ApplicationUser properties to JWT claim types (OpenID Connect standard claims)
SOURCE: `Services/ProfileService.cs`
CALLS:
  - UserManager — to retrieve ApplicationUser by subject ID and security stamp
CALLED_BY:
  - IdentityServer — during token issuance to populate GetProfileDataAsync and IsActiveAsync

---

COMPONENT_NAME: EFLoginService
TYPE: Service
PURPOSE: Validates user credentials and establishes authenticated sessions.
RESPONSIBILITIES:
  - Finds ApplicationUser by email address (username)
  - Checks password hash against stored credentials using UserManager
  - Signs in the user and establishes an authenticated cookie session
  - Supports both implicit sign-in and sign-in with explicit AuthenticationProperties
SOURCE: `Services/EFLoginService.cs`
CALLS:
  - UserManager — to find user by email and check password
  - SignInManager — to establish authenticated session
CALLED_BY:
  - AccountController — during login form POST processing

---

COMPONENT_NAME: RedirectService
TYPE: Utility
PURPOSE: Extracts and validates redirect URIs from OAuth 2.0 authorization request parameters.
RESPONSIBILITIES:
  - Parses the redirect_uri parameter from authorization request URLs
  - Decodes HTML entities to recover the original URI value
  - Validates that the redirect URI matches registered client redirect URIs
  - Prevents open redirect vulnerabilities by strict URI parsing
SOURCE: `Services/RedirectService.cs`
CALLS:
  - NONE
CALLED_BY:
  - AccountController — to validate redirect_uri during login callback

---

COMPONENT_NAME: ApplicationUser
TYPE: Store
PURPOSE: Represents a user identity with extended payment and shipping profile information.
RESPONSIBILITIES:
  - Extends IdentityUser with payment card details (number, expiration, CVV, cardholder name, card type)
  - Stores shipping address fields (street, city, state, country, zip code)
  - Provides user name and email for authentication
  - Supports security stamp for invalidating tokens on password or lockout changes
SOURCE: `Models/ApplicationUser.cs`
CALLS:
  - NONE
CALLED_BY:
  - UserManager — during credential validation and user lookup in EFLoginService and ProfileService
  - ApplicationDbContext — for persistence operations

---

COMPONENT_NAME: ApplicationDbContext
TYPE: Repository
PURPOSE: Abstracts Entity Framework Core access to ASP.NET Identity tables and ApplicationUser extension properties.
RESPONSIBILITIES:
  - Provides DbSet collections for ApplicationUser and IdentityRole
  - Manages ORM mapping of ApplicationUser properties to PostgreSQL tables
  - Supports automatic migration application on startup
  - Provides transactional data access for user queries and updates
SOURCE: `Data/ApplicationDbContext.cs`
CALLS:
  - NONE
CALLED_BY:
  - UserManager — for all user persistence operations (FindByIdAsync, FindByEmailAsync, CheckPasswordAsync, etc.)
  - SignInManager — for managing authentication claims and cookies

---

COMPONENT_NAME: UsersSeed
TYPE: Utility
PURPOSE: Initializes the identity database with a default test user on application startup.
RESPONSIBILITIES:
  - Checks for existing test user (alice) in the database
  - Creates a seeded ApplicationUser with pre-populated payment and shipping data if not present
  - Creates password hash for the test user (Pass123$)
  - Logs seed success or skip to the ILogger
SOURCE: `UsersSeed.cs`
CALLS:
  - UserManager — to find existing user and create new user with password hash
CALLED_BY:
  - ServiceDefaults — as part of automatic database migration and seeding on application startup

---

## Component Call Sequences

### Use-Case: User Login with OAuth 2.0 Authorization Code Flow

STEP 1: WebApp Client (External) → AccountController
  OPERATION: GET /Account/Login?returnUrl=...
  PURPOSE: Initiate login flow; return login form to user
  SOURCE: `Quickstart/Account/AccountController.cs` Line 42-50

STEP 2: User (Browser) → AccountController
  OPERATION: POST /Account/Login with username and password
  PURPOSE: Submit credentials for validation
  SOURCE: `Quickstart/Account/AccountController.cs` Line 58-60

STEP 3: AccountController → AccountController
  OPERATION: Retrieve AuthorizationContext from returnUrl
  PURPOSE: Determine which client application initiated the login request
  SOURCE: `Quickstart/Account/AccountController.cs` Line 65-66

STEP 4: AccountController → EFLoginService
  OPERATION: FindByUsername(username) / ValidateCredentials(user, password)
  PURPOSE: Look up user by email and validate password hash
  SOURCE: `Services/EFLoginService.cs` Line 15-20

STEP 5: EFLoginService → UserManager
  OPERATION: FindByEmailAsync(email) / CheckPasswordAsync(user, password)
  PURPOSE: Query database for user and verify password
  SOURCE: `Services/EFLoginService.cs` Line 15

STEP 6: EFLoginService → SignInManager
  OPERATION: SignInAsync(user, authenticationProperties)
  PURPOSE: Create authenticated cookies and establish session
  SOURCE: `Services/EFLoginService.cs` Line 30

STEP 7: AccountController → IdentityServer
  OPERATION: Implicit delegation via return View (signin-oidc)
  PURPOSE: Return authorization callback with authenticated session to client application
  SOURCE: `Quickstart/Account/AccountController.cs` Line 75+

---

### Use-Case: Issue Access Token for Downstream API

STEP 1: OAuth 2.0 Client (MAUI/WebApp) → IdentityServer
  OPERATION: POST /connect/token with authorization code
  PURPOSE: Exchange authorization code for access and ID tokens
  SOURCE: `Program.cs` Line 29

STEP 2: IdentityServer → ProfileService
  OPERATION: GetProfileDataAsync(context), IsActiveAsync(context)
  PURPOSE: Populate token claims and verify user is active (not locked/deleted)
  SOURCE: `Services/ProfileService.cs` Line 11-58

STEP 3: ProfileService → UserManager
  OPERATION: FindByIdAsync(subjectId) / GetSecurityStampAsync(user)
  PURPOSE: Load user from database and verify security stamp
  SOURCE: `Services/ProfileService.cs` Line 22-38

STEP 4: IdentityServer → IdentityServer
  OPERATION: Create JWT with claims (sub, preferred_username, openid, profile)
  PURPOSE: Issue access token valid for 2 hours with configured scopes
  SOURCE: `Program.cs` Line 23 (AccessTokenLifetime = 60*60*2)

STEP 5: IdentityServer → OAuth 2.0 Client
  OPERATION: Return token response (access_token, id_token, expires_in)
  PURPOSE: Provide signed JWT for calling downstream APIs
  SOURCE: `Program.cs` Line 29

---

## Communication Channels

CHANNEL_TYPE: HTTP (OAuth 2.0 Authorization Endpoint)
ENDPOINT: POST /connect/authorize
SOURCE: `Program.cs` Line 29 (IdentityServer registration)
NOTES:
  - Accepts authorization requests from OAuth 2.0 clients
  - Requires Authorization Code flow (all 6 clients configured with AllowedGrantTypes = GrantTypes.Code)
  - Returns authorization code in redirect to client

---

CHANNEL_TYPE: HTTP (OAuth 2.0 Token Endpoint)
ENDPOINT: POST /connect/token
SOURCE: `Program.cs` Line 29 (IdentityServer registration)
NOTES:
  - Accepts token requests with authorization code or refresh token
  - Returns access_token (JWT), id_token, refresh_token, expires_in
  - Client authentication via ClientId/ClientSecret (shared secret)

---

CHANNEL_TYPE: HTTP (User Authentication UI)
ENDPOINT: GET /Account/Login, POST /Account/Login, GET/POST /Consent/Index
SOURCE: `Quickstart/Account/AccountController.cs`, `Quickstart/Consent/ConsentController.cs`
NOTES:
  - Browser-based form submission for credentials
  - POST includes ValidateAntiForgeryToken (CSRF protection)
  - Supports returnUrl parameter for authorization request roundtrips

---

CHANNEL_TYPE: HTTP (Logout / Sign-Out)
ENDPOINT: GET /Account/Logout, POST /Account/LogoutAsync
SOURCE: `Quickstart/Account/AccountController.cs`
NOTES:
  - Clears authenticated session
  - Optionally includes id_token_hint and post_logout_redirect_uri per OIDC

---

CHANNEL_TYPE: HTTP (OAuth 2.0 UserInfo Endpoint)
ENDPOINT: GET /connect/userinfo (implicit)
SOURCE: `Program.cs` Line 29 (IdentityServer registration)
NOTES:
  - Returns authenticated user's claims given Bearer token
  - Used by clients to retrieve user profile after token exchange

---

CHANNEL_TYPE: HTTP (JWT Validation)
ENDPOINT: GET /.well-known/openid-configuration (OIDC Discovery)
SOURCE: `Program.cs` Line 29 (IdentityServer registration)
NOTES:
  - Provides metadata for JWT validation and endpoint discovery
  - Includes issuer, JWKS endpoint, authorization endpoint, token endpoint, etc.
  - Consumed by downstream services to validate bearer tokens

---

## Dependency Registration and Wiring

DI_CONTAINER: Microsoft.Extensions.DependencyInjection (ASP.NET Core native)

REGISTRATION_FILE: `Program.cs` Lines 1–48

REGISTRATIONS:

1. **IIdentityServerInteractionService**
   - LIFETIME: Scoped
   - ABSTRACTION: IIdentityServerInteractionService
   - IMPLEMENTATION: IdentityServer.AspNetCore.Services.IdentityServerInteractionService (internal)
   - SOURCE: `Program.cs` Line 27 (builder.Services.AddIdentityServer(...))
   - SNIPPET:
     ```csharp
     builder.Services.AddIdentityServer(options => { ... })
       .AddInMemoryIdentityResources(Config.GetResources())
       .AddInMemoryApiScopes(Config.GetApiScopes())
       .AddInMemoryApiResources(Config.GetApis())
       .AddInMemoryClients(Config.GetClients(builder.Configuration))
       .AddAspNetIdentity<ApplicationUser>();
     ```

2. **IProfileService**
   - LIFETIME: Transient
   - ABSTRACTION: IProfileService
   - IMPLEMENTATION: ProfileService
   - SOURCE: `Program.cs` Line 43
   - SNIPPET:
     ```csharp
     builder.Services.AddTransient<IProfileService, ProfileService>();
     ```

3. **ILoginService&lt;ApplicationUser&gt;**
   - LIFETIME: Transient
   - ABSTRACTION: ILoginService&lt;ApplicationUser&gt;
   - IMPLEMENTATION: EFLoginService
   - SOURCE: `Program.cs` Line 44
   - SNIPPET:
     ```csharp
     builder.Services.AddTransient<ILoginService<ApplicationUser>, EFLoginService>();
     ```

4. **IRedirectService**
   - LIFETIME: Transient
   - ABSTRACTION: IRedirectService
   - IMPLEMENTATION: RedirectService
   - SOURCE: `Program.cs` Line 45
   - SNIPPET:
     ```csharp
     builder.Services.AddTransient<IRedirectService, RedirectService>();
     ```

5. **UserManager&lt;ApplicationUser&gt;**
   - LIFETIME: Scoped
   - ABSTRACTION: UserManager&lt;ApplicationUser&gt;
   - IMPLEMENTATION: UserManager&lt;ApplicationUser&gt; (built-in ASP.NET Identity)
   - SOURCE: `Program.cs` Line 15 (builder.Services.AddIdentity(...))
   - SNIPPET:
     ```csharp
     builder.Services.AddIdentity<ApplicationUser, IdentityRole>()
       .AddEntityFrameworkStores<ApplicationDbContext>()
       .AddDefaultTokenProviders();
     ```

6. **SignInManager&lt;ApplicationUser&gt;**
   - LIFETIME: Scoped
   - ABSTRACTION: SignInManager&lt;ApplicationUser&gt;
   - IMPLEMENTATION: SignInManager&lt;ApplicationUser&gt; (built-in ASP.NET Identity)
   - SOURCE: `Program.cs` Line 15
   - SNIPPET:
     ```csharp
     builder.Services.AddIdentity<ApplicationUser, IdentityRole>()
       .AddEntityFrameworkStores<ApplicationDbContext>()
       .AddDefaultTokenProviders();
     ```

7. **ApplicationDbContext**
   - LIFETIME: Scoped
   - ABSTRACTION: ApplicationDbContext
   - IMPLEMENTATION: ApplicationDbContext
   - SOURCE: `Program.cs` Line 7 (builder.AddNpgsqlDbContext&lt;ApplicationDbContext&gt;(...))
   - SNIPPET:
     ```csharp
     builder.AddNpgsqlDbContext<ApplicationDbContext>("identitydb");
     ```

---

## Configuration and Secrets

SOURCE_TYPE: Configuration File (appsettings.json)
KEYS:
  - `Logging:LogLevel:Default` — Log level for application (default: Information)
  - `Logging:LogLevel:Microsoft.AspNetCore` — Log level for framework (default: Warning)
  - `MauiCallback` — OAuth 2.0 redirect URI for MAUI mobile client (value: maui://authcallback)
  - `UseCustomizationData` — Feature flag for customization data (default: false)
  - `TokenLifetimeMinutes` — Access token lifetime in minutes (default: 120)
  - `PermanentTokenLifetimeDays` — Refresh token lifetime in days (default: 365)
SENSITIVE: NO
LOCATION: `appsettings.json`

---

SOURCE_TYPE: Configuration File (appsettings.Development.json — development only)
KEYS:
  - `ConnectionStrings:IdentityDB` — PostgreSQL connection string for identity database
SENSITIVE: YES (contains plaintext password `yourWeak(!)Password` for development)
LOCATION: `appsettings.Development.json` (NOT committed to production)
NOTES: Development password intentionally weak; production must use secure secrets manager

---

SOURCE_TYPE: User Secrets (ASP.NET Core User Secrets Manager for local development)
KEYS: NONE (explicitly managed via appsettings files in this project)
SENSITIVE: OPTIONAL
LOCATION: `%APPDATA%\Microsoft\UserSecrets\aspnet-eShopOnContainers.Identity-90487118-103c-4ff0-b9da-e5e26f7ab0c5\` (Windows)
NOTES: UserSecretsId defined in `Identity.API.csproj` Line 4

---

SOURCE_TYPE: Environment Variables
KEYS: NONE (IdentityServer and ASP.NET Identity configuration is code-based via Config.cs and Program.cs)
SENSITIVE: OPTIONAL
LOCATION: System environment or Docker container environment
NOTES: Orchestration (eShop.AppHost) may inject CONNECTIONSTRINGS__IDENTITYDB at runtime

---

SOURCE_TYPE: Code-Based Configuration (IdentityServer)
KEYS:
  - **API Resources** (in-memory):
    - `orders` — OrdersAPI
    - `basket` — BasketAPI
    - `webhooks` — Webhooks registration service
  - **API Scopes** (in-memory):
    - `orders` — Scope to call Ordering API
    - `basket` — Scope to call Basket API
    - `webhooks` — Scope to call Webhooks API
  - **Identity Resources** (in-memory):
    - `openid` — OpenID Connect identifier
    - `profile` — User profile claims
  - **OAuth 2.0 Clients** (in-memory, 6 total; see Config.cs below):
    1. **maui** — MAUI mobile client (Authorization Code + PKCE, offline access)
    2. **webapp** — Web app client (Authorization Code, implicit)
    3. **webhooks** — Webhooks service (Client Credentials)
    4. **swagger** (Catalog, Ordering, Basket) — Swagger UI clients (Authorization Code)
SENSITIVE: Includes ClientSecrets (hashed)
LOCATION: `Configuration/Config.cs`
NOTES: In-memory database ideal for development/testing; production must use persisted configuration

---

SOURCE_TYPE: Code-Based Configuration (Token Lifetime)
KEYS:
  - `options.Authentication.CookieLifetime` — Session cookie lifetime = 2 hours
  - `AccessTokenLifetime` — JWT access token lifetime per client = 2 hours (MAUI, WebApp)
  - `IdentityTokenLifetime` — OIDC ID token lifetime per client = 2 hours (MAUI, WebApp)
SENSITIVE: NO
LOCATION: `Program.cs` Lines 23–24, `Configuration/Config.cs` (per-client)

---

## Persistence and Data Access

DATABASE: PostgreSQL
DATABASE_ENGINE: PostgreSQL 15+ (common in eShop deployments)
HOST: localhost (development); injected via ConnectionStrings:IdentityDB (production)

---

DATA_ACCESS: Entity Framework Core (EF Core) with PostgreSQL provider
ORM: Entity Framework Core 10.0+ (Npgsql.EntityFrameworkCore.PostgreSQL provider)
QUERY_BUILDER: LINQ to Entities via UserManager&lt;ApplicationUser&gt; (query abstraction)
DRIVER: Npgsql (PostgreSQL ADO.NET provider)

---

MIGRATIONS_PATH: IMPLIED. Migrations are managed automatically via EF Core Migrations framework.
NOTES: `Program.cs` Line 8 enables automatic migration application on startup (NOT recommended for production).

---

REPOSITORY_PATTERN: NO (ASP.NET Identity abstracts repository layer)
DATA_ACCESS_LAYER:
  - UserManager&lt;ApplicationUser&gt; (IUserStore&lt;ApplicationUser&gt; interface abstraction)
    - Encapsulates user CRUD, password hashing, security stamp management
    - Used by EFLoginService and ProfileService
  - SignInManager&lt;ApplicationUser&gt; (authentication state management)
    - Encapsulates cookie session creation, sign-in/sign-out
  - ApplicationDbContext (EF Core DbContext)
    - Directly inherits from IdentityDbContext&lt;ApplicationUser&gt;
    - Maps ApplicationUser and IdentityRole to PostgreSQL tables

---

TABLES (managed by ASP.NET Identity):
  - `AspNetUsers` — ApplicationUser instances with extended columns:
    - CardNumber, SecurityNumber, Expiration, CardHolderName, CardType (payment)
    - Street, City, State, Country, ZipCode (shipping address)
    - Name, LastName (profile)
  - `AspNetRoles` — IdentityRole instances (currently empty; no role-based access control in use)
  - `AspNetUserRoles` — User-to-role mapping (currently empty)
  - `AspNetUserLogins` — External login mappings (currently empty)
  - `AspNetUserTokens` — Personal data erasure and recovery tokens
  - `AspNetUserClaims` — User claims (rarely used in this design)

---

QUERY EXAMPLES:
  ```csharp
  // EFLoginService
  var user = await _userManager.FindByEmailAsync(username);  // Query AspNetUsers by email
  var valid = await _userManager.CheckPasswordAsync(user, password);  // Verify password hash
  ```
  ```csharp
  // ProfileService
  var user = await _userManager.FindByIdAsync(subjectId);  // Query AspNetUsers by ID
  var stamp = await _userManager.GetSecurityStampAsync(user);  // Retrieve security stamp
  ```

---

## Patterns and Architecture Notes

PATTERN: OAuth 2.0 Authorization Code Flow (with PKCE for public clients)
EVIDENCE: `Configuration/Config.cs` Lines 40–88 (MAUI client with GrantTypes.Code + RequirePkce = true)
SNIPPET:
  ```csharp
  new Client
  {
      ClientId = "maui",
      AllowedGrantTypes = GrantTypes.Code,
      RequirePkce = true,
      RedirectUris = { configuration["MauiCallback"] },
      AllowOfflineAccess = true
  }
  ```
NOTES:
  - PKCE protects mobile and single-page application clients from authorization code interception
  - Offline access enables refresh token rotation
  - All 6 clients use Authorization Code variant (most secure for browser/mobile)

---

PATTERN: OpenID Connect (OIDC) Identity Provider
EVIDENCE: `Program.cs` Lines 27–41 (IdentityServer configuration with identity resources and ASP.NET Identity integration)
SNIPPET:
  ```csharp
  builder.Services.AddIdentityServer(options => { ... })
    .AddInMemoryIdentityResources(Config.GetResources())
    .AddInMemoryClients(Config.GetClients(builder.Configuration))
    .AddAspNetIdentity<ApplicationUser>();
  ```
NOTES:
  - Complies with OpenID Connect 1.0 specification
  - Issues ID tokens (claims about end-user identity) and access tokens (API authorization)
  - Provides /.well-known/openid-configuration for automated client discovery and JWT validation

---

PATTERN: Distributed Authentication (Microservices)
EVIDENCE: `Configuration/Config.cs` (multiple API resources and scopes defined for Ordering, Basket, Webhooks APIs)
SNIPPET:
  ```csharp
  public static IEnumerable<ApiResource> GetApis()
  {
      return new List<ApiResource>
      {
          new ApiResource("orders", "Orders Service"),
          new ApiResource("basket", "Basket Service"),
          new ApiResource("webhooks", "Webhooks registration Service")
      };
  }
  ```
NOTES:
  - Central identity provider issues tokens that downstream services validate using JWKS (JSON Web Key Set)
  - Reduces credential management burden in individual microservices
  - Enables single sign-on (SSO) across eShop ecosystem

---

PATTERN: ASP.NET Identity with Entity Framework Core
EVIDENCE: `Program.cs` Lines 15 (AddIdentity), `Data/ApplicationDbContext.cs` (IdentityDbContext inheritance)
SNIPPET:
  ```csharp
  builder.Services.AddIdentity<ApplicationUser, IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();
  ```
NOTES:
  - Industry-standard pattern for user management in ASP.NET applications
  - Provides password hashing (PBKDF2), lockout policies, claims, and security stamps out-of-box
  - Extensible via inheritance (ApplicationUser adds payment/shipping fields)

---

PATTERN: Dependency Injection (Service Locator Anti-Pattern Avoided)
EVIDENCE: `Program.cs` Lines 7–45, Controllers accept dependencies via constructor injection
SNIPPET:
  ```csharp
  public class AccountController : Controller
  {
      private readonly UserManager<ApplicationUser> _userManager;
      private readonly SignInManager<ApplicationUser> _signInManager;
      private readonly IIdentityServerInteractionService _interaction;
      // ... constructor accepts all dependencies
  }
  ```
NOTES:
  - Uses constructor injection throughout; no service locator antipattern
  - Enables testability and loose coupling

---

## Security and Operational Considerations

AUTHN_AUTHZ: OAuth 2.0 Bearer Token (JWT)
MECHANISM: Authorization Header carries JWT issued by IdentityServer
CONFIGURATION_FILE: `Program.cs` Lines 27–41 (IdentityServer setup)
NOTES:
  - JWT signed with asymmetric key (RS256 algorithm; key generated via AddDeveloperSigningCredential)
  - Validated by downstream services using public key from JWKS endpoint
  - Includes claims: subject identifier (sub), preferred username, scopes, issue time (iat), expiration (exp)
  - Client authentication (server-to-server) uses ClientId + ClientSecret (shared secret hashed)

---

KNOWN_RISKS:

1. **Hardcoded Password in Development** — `appsettings.Development.json` Line 4
   - RISK: Development credentials `yourWeak(!)Password` committed to repository
   - MITIGATION: Use only for local development; never commit production credentials
   - REMEDIATION: Deploy with Azure Key Vault or equivalent secrets manager in production

2. **Developer Signing Credential** — `Program.cs` Line 41 (AddDeveloperSigningCredential)
   - RISK: AddDeveloperSigningCredential uses a temporary key (tempkey.jwk) suitable only for development
   - MITIGATION: Comment indicates TODO to switch to secure key material (e.g., certificate) for production
   - REMEDIATION: Replace with AddSigningCredential(certificate) and store in secure HSM or Key Vault in production

3. **In-Memory Client Configuration** — `Program.cs` Lines 30–36
   - RISK: Client definitions (OAuth clients, scopes, API resources) are in-memory and not persisted
   - MITIGATION: Suitable for development; production should use AddConfigurationStore() to persist in database
   - REMEDIATION: Migrate to IdentityServer.EntityFramework for production deployments

4. **KeyManagement Disabled** — `Program.cs` Line 22 (options.KeyManagement.Enabled = false)
   - RISK: Comment indicates this is a TODO for production
   - MITIGATION: Automatic key rotation is disabled for development simplicity
   - REMEDIATION: Enable automatic key rotation in production and store keys securely (not in repository)

5. **No Encryption for Sensitive User Fields** — `Models/ApplicationUser.cs` (CardNumber, SecurityNumber stored as plain strings)
   - RISK: Payment data (card number, CVV) stored in plaintext in PostgreSQL
   - MITIGATION: Development convenience; PCI DSS compliance not required for development
   - REMEDIATION: Implement column-level encryption or use tokenization for payment data in production

6. **AllowAccessTokensViaBrowser** — `Configuration/Config.cs` (client configuration)
   - RISK: MAUI and WebApp clients allow access tokens to be returned via browser redirects
   - MITIGATION: Combined with SameSiteMode.Lax cookie policy to reduce CSRF attack surface
   - REMEDIATION: Use response_mode=form_post or form_response_mode for sensitive clients; audit usage

---

OBSERVABILITY:

LOGGING_FRAMEWORK: Microsoft.Extensions.Logging (built-in ASP.NET Core)
CONFIGURATION:
  - Default log level: Information
  - Framework (Microsoft.AspNetCore) log level: Warning
  - Source: `appsettings.json` Lines 2–5
  - Implementation: Any ILogger&lt;T&gt; dependency injected into controllers and services

LOGGING_LOCATIONS:
  - `UsersSeed.cs` Line 32 — Debug log when test user created/found
  - `AccountController.cs` — No explicit logging in code snippet (event logging via IEventService)
  - `ProfileService.cs` — No explicit logging (exception thrown on invalid subject)
  - IdentityServer internal — Configurable via options.Events.Raise* flags in `Program.cs` Lines 20–25

EVENT_SERVICE: IEventService (IdentityServer built-in)
  - Configured to raise Error, Information, Failure, and Success events
  - Used by ConsentController to log consent acceptance/denial
  - Source: `Program.cs` Lines 20–25

HEALTH_CHECK_ENDPOINTS: NONE (health checks not explicitly configured in this component)
NOTES: eShop.AppHost orchestrator may define health checks at a higher level

DISTRIBUTED_TRACING: NONE (OpenTelemetry not configured in snapshot)
NOTES: eShop.ServiceDefaults may provide tracing instrumentation via builder.AddServiceDefaults()

METRICS: NONE (explicit metrics not configured in snapshot)
NOTES: ASP.NET Core provides implicit HTTP request metrics (latency, request count) via runtime instrumentation

---

DEPLOYMENT:

DOCKER_FILE: `src/Identity.API/Dockerfile` (inferred location; not included in workspace snapshot)
COMPOSE_FILE: NONE (docker-compose at project root; see `eShop.sln` orchestration)
ORCHESTRATION_FILE: `eShop.AppHost/Program.cs` (Aspire .NET orchestrator)
KUBERNETES_MANIFEST: NONE (Kubernetes manifests not in snapshot; may be in ci/cd pipeline)

DEPLOYMENT_CONFIGURATION:
  - **Container Runtime**: ASP.NET Core 10.0 runtime
  - **Base Image**: mcr.microsoft.com/dotnet/aspnet:10.0 (inferred from .NET 10 TFM)
  - **Port**: 5039 (default; negotiated via eShop.AppHost)
  - **Database**: PostgreSQL 15+ (service defined in orchestration)
  - **Environment Variables**:
    - `CONNECTIONSTRINGS__IDENTITYDB` — PostgreSQL connection string (injected at runtime)
    - `ASPNETCORE_ENVIRONMENT` — Development, Staging, or Production

STARTUP_SEQUENCE:
  1. ASP.NET Core host initializes with Program.cs configuration
  2. IdentityServer service container registers all dependencies
  3. ApplicationDbContext applied via AddMigration (automatic migration on startup)
  4. UsersSeed seeder runs, creating default test user if not present
  5. HTTP server listens on configured port (5039)
  6. /.well-known/openid-configuration endpoint available for client discovery

---

END OF COMPONENT OVERVIEW
