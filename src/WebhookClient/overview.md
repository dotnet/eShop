# WebhookClient

Web application that receives and displays webhook notifications from the eShop system, specifically tracking order payment events through webhook subscriptions.

## Summary

- ASP.NET Core Blazor Server web application for managing and monitoring webhook subscriptions and received webhook messages
- Integrates with OpenID Connect for user authentication via Identity.API
- Communicates with Webhooks.API to register and retrieve webhook subscriptions
- Uses in-memory repository pattern to store received webhook events
- Provides HTTP endpoints to receive webhook callbacks and a UI to view registered subscriptions and received messages

## Projects and Folder Map

| PATH | PURPOSE | ENTRY_FILES |
|------|---------|------------|
| `src/WebhookClient` | Main Blazor Server application for webhook management and monitoring | `Program.cs`, `App.razor` |
| `src/WebhookClient/Components` | Razor components for UI rendering (pages, layout, routing) | `App.razor`, `Routes.razor`, `Layout/MainLayout.razor` |
| `src/WebhookClient/Endpoints` | HTTP endpoint definitions for webhook reception and authentication flows | `WebhookEndpoints.cs`, `AuthenticationEndpoints.cs` |
| `src/WebhookClient/Services` | Core business logic including HTTP clients, data models, and in-memory storage | `HooksRepository.cs`, `WebhooksClient.cs`, `WebHookReceived.cs` |
| `src/WebhookClient/Extensions` | Dependency injection and configuration extensions | `Extensions.cs` |
| `src/WebhookClient/Components/Pages` | Razor page components for user-facing features | `Home.razor`, `AddWebhook.razor` |
| `src/WebhookClient/Components/Pages/Home` | Sub-pages for viewing registered hooks and received messages | `RegisteredHooks.razor`, `ReceivedMessages.razor` |
| `src/WebhookClient/Components/Layout` | Layout templates and user menu component | `MainLayout.razor`, `UserMenu.razor` |

## Components

COMPONENT_NAME: WebhookClientApplication
TYPE: API
PURPOSE: Entry point for the Blazor Server web application; configures middleware, routes, and services.
RESPONSIBILITIES:
  - Initializes service collection and dependency injection container
  - Configures authentication and authorization pipelines
  - Maps HTTP endpoints for webhooks and authentication
  - Renders Razor components with interactive server rendering
SOURCE: `Program.cs`
CALLS:
  - WebhookEndpoints — to map webhook HTTP endpoints
  - AuthenticationEndpoints — to map authentication HTTP endpoints
  - Extensions.AddApplicationServices — to configure application services
  - RazorComponents — to render interactive server components
CALLED_BY:
  - NONE (ASP.NET Core host)

---

COMPONENT_NAME: WebhookEndpoints
TYPE: Gateway
PURPOSE: HTTP endpoint handler for receiving incoming webhook events and validating webhook health checks.
RESPONSIBILITIES:
  - Handles OPTIONS /check endpoint for webhook health verification with token validation
  - Handles POST /webhook-received endpoint to accept incoming webhook payloads
  - Validates incoming webhook tokens against configured token
  - Persists received webhook data to HooksRepository
  - Logs webhook reception and validation events
SOURCE: `Endpoints/WebhookEndpoints.cs`
CALLS:
  - HooksRepository — to store received webhook events
  - ILogger — to log webhook events
  - IConfiguration — to retrieve token validation settings
CALLED_BY:
  - WebhookClientApplication (via MapWebhookEndpoints)

---

COMPONENT_NAME: AuthenticationEndpoints
TYPE: Gateway
PURPOSE: HTTP endpoint handler for user authentication flows.
RESPONSIBILITIES:
  - Handles POST /logout endpoint to sign out users
  - Validates antiforgery tokens before logout
  - Signs out from both Cookie and OpenID Connect authentication schemes
SOURCE: `Endpoints/AuthenticationEndpoints.cs`
CALLS:
  - HttpContext.SignOutAsync — to sign out from authentication schemes
  - IAntiforgery — to validate antiforgery tokens
CALLED_BY:
  - WebhookClientApplication (via MapAuthenticationEndpoints)

---

COMPONENT_NAME: HooksRepository
TYPE: Repository
PURPOSE: In-memory store for webhook events received by the application.
RESPONSIBILITIES:
  - Maintains a thread-safe queue of received webhook events (WebHookReceived objects)
  - Allows persistence of new webhook events via AddNew()
  - Retrieves all stored webhook events via GetAll()
  - Provides pub/sub notification mechanism for subscribers to be notified of new events
  - Manages subscriber lifecycle and callback invocation
SOURCE: `Services/HooksRepository.cs`
CALLS:
  - NONE (internal collections only)
CALLED_BY:
  - WebhookEndpoints — to store received webhook events
  - ReceivedMessages — to retrieve and monitor webhook events

---

COMPONENT_NAME: WebhooksClient
TYPE: Service
PURPOSE: HTTP client wrapper for communicating with Webhooks.API.
RESPONSIBILITIES:
  - Registers new webhook subscriptions via POST /api/webhooks
  - Retrieves list of currently registered webhooks via GET /api/webhooks
  - Handles JSON serialization/deserialization of webhook requests and responses
SOURCE: `Services/WebhooksClient.cs`
CALLS:
  - HttpClient — to send HTTP requests to Webhooks.API
CALLED_BY:
  - AddWebhook page component — to register new subscription
  - RegisteredHooks component — to load registered webhooks

---

COMPONENT_NAME: WebhookClientOptions
TYPE: Utility
PURPOSE: Configuration model for webhook client settings.
RESPONSIBILITIES:
  - Stores webhook client configuration values
  - Provides token for webhook authentication
  - Stores self-URL for webhook callback registration
  - Indicates whether token validation is enabled
SOURCE: `Services/WebhookClientOptions.cs`
CALLS:
  - NONE
CALLED_BY:
  - Extensions.AddApplicationServices — configuration binding
  - AddWebhook page component — to read configured token
  - WebhookEndpoints — to validate incoming tokens

---

COMPONENT_NAME: WebHookReceived
TYPE: Utility
PURPOSE: Data model representing a received webhook event.
RESPONSIBILITIES:
  - Encapsulates webhook event data including timestamp, payload, and token
SOURCE: `Services/WebHookReceived.cs`
CALLS:
  - NONE
CALLED_BY:
  - HooksRepository — as stored data type
  - WebhookEndpoints — to create instances from incoming requests
  - ReceivedMessages — to display in UI

---

COMPONENT_NAME: WebhookData
TYPE: Utility
PURPOSE: Data model for webhook event payload from external sources.
RESPONSIBILITIES:
  - Represents incoming webhook data structure with timestamp, payload, and event type
SOURCE: `Services/WebhookData.cs`
CALLS:
  - NONE
CALLED_BY:
  - WebhookEndpoints — to deserialize incoming HTTP request body

---

COMPONENT_NAME: WebhookSubscriptionRequest
TYPE: Utility
PURPOSE: Data model for webhook subscription registration requests.
RESPONSIBILITIES:
  - Encapsulates subscription request parameters (URL, token, event type, grant URL)
  - Used when registering new webhooks with Webhooks.API
SOURCE: `Services/WebhookSubscriptionRequest.cs`
CALLS:
  - NONE
CALLED_BY:
  - AddWebhook page component — to construct subscription request payload
  - WebhooksClient — to send subscription requests

---

COMPONENT_NAME: WebhookResponse
TYPE: Utility
PURPOSE: Data model for webhook registration details returned from Webhooks.API.
RESPONSIBILITIES:
  - Represents registered webhook subscription data (date, destination URL, token)
SOURCE: `Services/WebhookResponse.cs`
CALLS:
  - NONE
CALLED_BY:
  - WebhooksClient — to deserialize API responses
  - RegisteredHooks component — to display registered webhooks

---

COMPONENT_NAME: WebhookType
TYPE: Utility
PURPOSE: Enumeration of supported webhook event types.
RESPONSIBILITIES:
  - Defines webhook event categories in the system
SOURCE: `Services/WebhookType.cs`
CALLS:
  - NONE
CALLED_BY:
  - Components and services that filter or categorize webhook types

---

COMPONENT_NAME: Extensions
TYPE: Utility
PURPOSE: Dependency injection and configuration extensions for the application.
RESPONSIBILITIES:
  - Registers application services into the service collection
  - Configures authentication and authorization
  - Configures OpenID Connect integration with Identity.API
  - Sets up HTTP client for WebhooksClient with automatic versioning and auth tokens
  - Configures Razor components and interactive server rendering
SOURCE: `Extensions/Extensions.cs`
CALLS:
  - IHostApplicationBuilder — to register services and configure authentication
  - HttpClient factory — to create typed HTTP client
CALLED_BY:
  - Program.cs — invoked during startup to configure services

---

COMPONENT_NAME: App
TYPE: Utility
PURPOSE: Root Razor component that initializes the Blazor application shell.
RESPONSIBILITIES:
  - Defines HTML document structure and layout
  - References CSS stylesheets and Blazor script
  - Renders routing and interactive components
SOURCE: `Components/App.razor`
CALLS:
  - Routes — to render routed components
  - HeadOutlet — to render head content
CALLED_BY:
  - Program.cs (MapRazorComponents)

---

COMPONENT_NAME: Routes
TYPE: Utility
PURPOSE: Router component that maps URLs to Razor pages.
RESPONSIBILITIES:
  - Handles URL routing and component resolution
  - Applies default layout (MainLayout) to routed pages
  - Enables focus management for accessibility
SOURCE: `Components/Routes.razor`
CALLS:
  - MainLayout — as default layout for all routed pages
  - Page components — based on route matching
CALLED_BY:
  - App.razor

---

COMPONENT_NAME: MainLayout
TYPE: Utility
PURPOSE: Master layout template for all pages in the application.
RESPONSIBILITIES:
  - Renders page header with application title and user menu
  - Renders page body content
  - Displays error UI for unhandled errors
SOURCE: `Components/Layout/MainLayout.razor`
CALLS:
  - UserMenu — to render user authentication menu
  - Body — to render page-specific content
CALLED_BY:
  - Routes.razor (as default layout)

---

COMPONENT_NAME: UserMenu
TYPE: Utility
PURPOSE: UI component for authentication state and user actions.
RESPONSIBILITIES:
  - Displays logged-in username when authenticated
  - Shows logout button for authenticated users
  - Shows login button for unauthenticated users
  - Constructs login redirect URL with return parameter
SOURCE: `Components/Layout/UserMenu.razor`
CALLS:
  - NavigationManager — to navigate to login page or construct URLs
  - AuthorizeView — to conditionally render based on auth state
CALLED_BY:
  - MainLayout.razor

---

COMPONENT_NAME: HomePage
TYPE: Utility
PURPOSE: Home page displaying webhook subscriptions and received messages.
RESPONSIBILITIES:
  - Shows list of registered webhook subscriptions (RegisteredHooks component)
  - Shows list of received webhook messages (ReceivedMessages component)
  - Provides link to add new webhook subscription
  - Guards content behind authorization view
SOURCE: `Components/Pages/Home/Home.razor`
CALLS:
  - RegisteredHooks — to display registered subscriptions
  - ReceivedMessages — to display received webhook events
  - AuthorizeView — to control visibility based on authentication
CALLED_BY:
  - Routes.razor (via URL "/" routing)

---

COMPONENT_NAME: RegisteredHooks
TYPE: Utility
PURPOSE: Component to display registered webhook subscriptions.
RESPONSIBILITIES:
  - Loads webhook subscriptions from Webhooks.API on initialization
  - Displays subscriptions in a grid with columns: Date, Destination, Token
  - Provides sorting capability on grid columns
SOURCE: `Components/Pages/Home/RegisteredHooks.razor`
CALLS:
  - WebhooksClient — to load registered webhooks
  - QuickGrid — to render data grid
CALLED_BY:
  - HomePage.razor

---

COMPONENT_NAME: ReceivedMessages
TYPE: Utility
PURPOSE: Component to display received webhook messages with real-time updates.
RESPONSIBILITIES:
  - Loads all currently received webhook events on initialization
  - Subscribes to HooksRepository notifications for new events
  - Updates UI when new webhook events are received
  - Displays messages in a grid with columns: When, Data, Token
  - Provides sorting capability on grid columns
  - Cleans up subscription on component disposal
SOURCE: `Components/Pages/Home/ReceivedMessages.razor`
CALLS:
  - HooksRepository — to retrieve initial messages and subscribe to updates
  - HooksRepository.Subscribe — to register change notification callback
  - QuickGrid — to render data grid
CALLED_BY:
  - HomePage.razor

---

COMPONENT_NAME: AddWebhookPage
TYPE: Utility
PURPOSE: Component to register a new webhook subscription.
RESPONSIBILITIES:
  - Displays form to input webhook token
  - Pre-fills token from WebhookClientOptions configuration
  - Constructs webhook subscription request with event type "OrderPaid"
  - Calls WebhooksClient to register subscription with Webhooks.API
  - Navigates to home page on successful registration
  - Displays error message if registration fails
SOURCE: `Components/Pages/AddWebhook.razor`
CALLS:
  - WebhooksClient — to submit webhook subscription
  - WebhookClientOptions — to read pre-configured token and self-URL
  - NavigationManager — to construct callback URLs and navigate
CALLED_BY:
  - Routes.razor (via URL "/add-webhook" routing)

## Component Call Sequences

### Use-Case: Register Webhook Subscription

STEP 1: AddWebhookPage → WebhookClientOptions
  OPERATION: Read Token, SelfUrl configuration
  PURPOSE: Retrieve pre-configured webhook token and self-URL for constructing callback endpoint
  SOURCE: `Components/Pages/AddWebhook.razor`

STEP 2: AddWebhookPage → NavigationManager
  OPERATION: Construct base URI and callback URLs
  PURPOSE: Build check and webhook-received endpoint URLs for the subscription request
  SOURCE: `Components/Pages/AddWebhook.razor`

STEP 3: AddWebhookPage → WebhooksClient
  OPERATION: AddWebHookAsync(WebhookSubscriptionRequest)
  PURPOSE: Submit subscription request to Webhooks.API to register the webhook
  SOURCE: `Components/Pages/AddWebhook.razor`

STEP 4: WebhooksClient → HttpClient
  OPERATION: PostAsJsonAsync(/api/webhooks, payload)
  PURPOSE: Send HTTP POST request to Webhooks.API with subscription details
  SOURCE: `Services/WebhooksClient.cs`

STEP 5: AddWebhookPage → NavigationManager
  OPERATION: NavigateTo("")
  PURPOSE: Navigate back to home page after successful webhook registration
  SOURCE: `Components/Pages/AddWebhook.razor`

### Use-Case: Receive and Display Webhook Event

STEP 1: WebhookEndpoints (HTTP endpoint) → WebhookEndpoints
  OPERATION: POST /webhook-received with WebhookData payload
  PURPOSE: Receive incoming webhook callback from external service
  SOURCE: `Endpoints/WebhookEndpoints.cs`

STEP 2: WebhookEndpoints → WebhookEndpoints
  OPERATION: Validate token from request header X-eshop-whtoken
  PURPOSE: Verify webhook authentication token against configured value
  SOURCE: `Endpoints/WebhookEndpoints.cs`

STEP 3: WebhookEndpoints → HooksRepository
  OPERATION: AddNew(WebHookReceived)
  PURPOSE: Store received webhook event in repository
  SOURCE: `Endpoints/WebhookEndpoints.cs`

STEP 4: HooksRepository → ReceivedMessages (subscribers)
  OPERATION: NotifyAsync() via subscription callback
  PURPOSE: Notify all subscribers that a new webhook event has been received
  SOURCE: `Services/HooksRepository.cs`

STEP 5: ReceivedMessages → HooksRepository
  OPERATION: GetAll()
  PURPOSE: Fetch updated list of all received webhook events
  SOURCE: `Components/Pages/Home/ReceivedMessages.razor`

STEP 6: ReceivedMessages → QuickGrid
  OPERATION: Render messages property
  PURPOSE: Update UI grid to display newly received webhook event
  SOURCE: `Components/Pages/Home/ReceivedMessages.razor`

## Communication Channels

CHANNEL_TYPE: HTTP
ENDPOINT: POST /webhook-received
SOURCE: `Endpoints/WebhookEndpoints.cs`
NOTES: Receives incoming webhook callbacks from external services; expects X-eshop-whtoken header for token validation; request body is WebhookData JSON; returns 200 OK with WebHookReceived response on success, 400 Bad Request if token validation fails

---

CHANNEL_TYPE: HTTP
ENDPOINT: OPTIONS /check
SOURCE: `Endpoints/WebhookEndpoints.cs`
NOTES: Webhook health check endpoint for verifying webhook connectivity; expects X-eshop-whtoken header; returns 200 OK if token is valid or validation is disabled, 400 Bad Request if token validation fails

---

CHANNEL_TYPE: HTTP
ENDPOINT: POST /logout
SOURCE: `Endpoints/AuthenticationEndpoints.cs`
NOTES: Logout endpoint for terminating user sessions; requires antiforgery token in request body; signs out from both Cookie and OpenID Connect schemes; redirect behavior determined by client

---

CHANNEL_TYPE: HTTP
ENDPOINT: GET/POST http://webhooks-api/api/webhooks
SOURCE: `Services/WebhooksClient.cs`, `Extensions/Extensions.cs`
NOTES: Client calls to Webhooks.API; GET retrieves registered webhooks as IEnumerable<WebhookResponse>; POST registers new webhooks via WebhookSubscriptionRequest; BaseAddress: http://webhooks-api; includes API version 1.0 and bearer auth token automatically

---

CHANNEL_TYPE: HTTP
ENDPOINT: OpenID Connect Authority/Token endpoints at Identity.API
SOURCE: `Extensions/Extensions.cs`
NOTES: OpenID Connect redirect-based authentication flow; Authority configured from IdentityUrl configuration; SignedOutRedirectUri from CallBackUrl; client ID: webhooksclient; scopes: openid, webhooks; saves tokens for authenticated user

## Dependency Registration and Wiring

DI_CONTAINER: ASP.NET Core built-in dependency injection (IServiceCollection, IHostApplicationBuilder)

REGISTRATION_FILE: `Extensions/Extensions.cs` method `AddApplicationServices(IHostApplicationBuilder builder)`

| Abstraction | Implementation | Lifetime | Source |
|------------|---------------|----------|--------|
| WebhookClientOptions | WebhookClientOptions | Singleton (via Options) | `Extensions/Extensions.cs` |
| HooksRepository | HooksRepository | Singleton | `Extensions/Extensions.cs` |
| HttpClient typed WebhooksClient | WebhooksClient | Transient (via factory) | `Extensions/Extensions.cs` |
| HttpClient configured with base address | HttpClient | Transient (via factory) | `Extensions/Extensions.cs` |
| AuthenticationStateProvider | ServerAuthenticationStateProvider | Scoped | `Extensions/Extensions.cs` |
| IAuthorizationPolicyProvider | (default) | Built-in | `Extensions/Extensions.cs` |
| RazorComponents | RazorComponents | Built-in | `Program.cs` |
| InteractiveServerComponents | InteractiveServerComponents | Built-in | `Program.cs` |

Snippet — WebhooksClient registration with API versioning:
```csharp
builder.Services.AddHttpClient<WebhooksClient>(o => o.BaseAddress = new("http://webhooks-api"))
    .AddApiVersion(1.0)
    .AddAuthToken();
```

Snippet — Options configuration:
```csharp
builder.Services.AddOptions<WebhookClientOptions>()
    .BindConfiguration(nameof(WebhookClientOptions));
```

Snippet — Authentication/Authorization setup:
```csharp
services.AddAuthorization();
services.AddAuthentication(options =>
{
    options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = OpenIdConnectDefaults.AuthenticationScheme;
})
.AddCookie(options => options.Cookie.Name = ".AspNetCore.WebHooksClientIdentity")
.AddOpenIdConnect(options => /* OpenID Connect configuration */);
services.AddScoped<AuthenticationStateProvider, ServerAuthenticationStateProvider>();
services.AddCascadingAuthenticationState();
```

## Configuration and Secrets

| SOURCE_TYPE | KEYS | SENSITIVE | LOCATION |
|------------|------|-----------|----------|
| config file | WebhookClientOptions:Token | YES | `appsettings.json` |
| config file | WebhookClientOptions:SelfUrl | NO | `appsettings.json` |
| config file | WebhookClientOptions:ValidateToken | NO | `appsettings.json` |
| config file | ValidateToken | NO | `appsettings.json` |
| environment variable | IdentityUrl | NO | Read by `Extensions.cs` via IConfiguration |
| environment variable | CallBackUrl | NO | Read by `Extensions.cs` via IConfiguration |
| environment variable | SessionCookieLifetimeMinutes | NO | Read by `Extensions.cs` via IConfiguration; default: 60 |
| hardcoded in code | ClientId: "webhooksclient" | NO | `Extensions.cs` (AddOpenIdConnect) |
| hardcoded in code | ClientSecret: "secret" | YES | `Extensions.cs` (AddOpenIdConnect) |

## Persistence and Data Access

DATABASE: NONE (in-memory only)

DATA_ACCESS: In-memory ConcurrentQueue<T> and ConcurrentDictionary<T, U> in HooksRepository

MIGRATIONS_PATH: NONE

REPOSITORY_PATTERN: YES

| Abstraction | Implementation | File Path |
|------------|---------------|-----------|
| HooksRepository (no interface) | HooksRepository | `Services/HooksRepository.cs` |

Repository Methods:
- `AddNew(WebHookReceived hook)` — enqueues received webhook and notifies subscribers
- `GetAll()` — retrieves all stored webhook events as IEnumerable
- `Subscribe(Func<Task> callback)` — registers subscriber callback for new event notifications; returns IDisposable for unsubscribe

## Patterns and Architecture Notes

| PATTERN | EVIDENCE | SNIPPET |
|---------|----------|---------|
| Repository Pattern | HooksRepository abstracts in-memory collection; AddNew() and GetAll() encapsulate storage logic | `Services/HooksRepository.cs` |
| Pub/Sub (Observer) | HooksRepository maintains ConcurrentDictionary of subscribers; notifies all on new events | Methods: Subscribe(), AddNew() with foreach loop calling NotifyAsync() |
| Dependency Injection | All components registered and resolved via IServiceCollection; Extensions.cs centralizes DI configuration | `Extensions/Extensions.cs` invoked in Program.cs |
| Typed HttpClient | WebhooksClient registered as typed HttpClient with automatic BaseAddress and auth token injection | `AddHttpClient<WebhooksClient>().AddApiVersion(1.0).AddAuthToken()` |
| Configuration Options Pattern | WebhookClientOptions bound to configuration section via IOptions<T> | `AddOptions<WebhookClientOptions>().BindConfiguration()` |
| OpenID Connect Authentication | Redirect-based OIDC flow with Cookie scheme for session management | `AddOpenIdConnect()` with Authority, ClientId, Scopes in Extensions.cs |

## Security and Operational Considerations

AUTHN_AUTHZ: 
- OpenID Connect Bearer tokens via Identity.API; configured in `Extensions/Extensions.cs` method `AddAuthenticationServices()`
- Webhook token validation via custom header X-eshop-whtoken in `Endpoints/WebhookEndpoints.cs`
- Authorization required for accessing registered webhooks and received messages (AuthorizeView guard)

KNOWN_RISKS:
- Hardcoded ClientSecret "secret" in `Extensions/Extensions.cs` OpenID Connect configuration (should use secure secrets management)
- Hardcoded webhook token in `appsettings.json` WebhookClientOptions:Token (sensitive configuration exposed in version control)
- No HTTPS enforcement in Development environment
- Token validation can be disabled via ValidateToken configuration flag, potentially allowing unauthorized webhook reception
- No rate limiting on /webhook-received endpoint; vulnerable to high-volume webhook flooding
- No filtering or retention policy on in-memory webhook queue; unbounded memory growth possible with high webhook volume

OBSERVABILITY:
- Logging framework: Microsoft.Extensions.Logging (standard ASP.NET Core logging)
- Log level configuration: Information for application logs, Warning for Microsoft.AspNetCore framework logs (see appsettings.json)
- Webhook reception logged at Information level: "Received hook with token...", "Received hook is going to be processed"
- Health check endpoint: `/health` (via MapDefaultEndpoints from ServiceDefaults)
- Structured logging: ILogger<T> injected into WebhookEndpoints for contextual logging

DEPLOYMENT:
- Dockerfile: Not present in WebhookClient folder; assumed to be in root build configuration
- Docker Compose: Not present; assumed to be in root compose files
- Kubernetes manifests: Not present; assumed to be in root k8s configuration
- Service registration: Connected via service discovery to webhooks-api (base address: http://webhooks-api)
