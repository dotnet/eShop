# WebApp — Interactive Blazor frontend for eShop multi-service e-commerce platform

## Summary

- Blazor Server interactive web application serving as the primary user interface for eShop
- Manages shopping cart, catalog browsing, checkout, and order placement workflows
- Communicates with backend services (Basket, Catalog, Ordering APIs) via gRPC and HTTP
- Consumes order status update events from RabbitMQ event bus
- Supports AI chatbot assistance powered by OpenAI or Ollama
- Handles user authentication via OpenIdConnect with identity server integration

## Projects and Folder Map

| PATH | PURPOSE | ENTRY_FILES |
|------|---------|-------------|
| `src/WebApp` | Blazor Server web application providing interactive UI for shopping experience | `Program.cs`, `App.razor`, `Components/Routes.razor` |
| `src/WebApp/Components` | Razor components for pages, layouts, and interactive UI elements | `App.razor`, `Routes.razor` |
| `src/WebApp/Components/Pages` | Page components organized by functional domain (Cart, Catalog, Checkout, Item, User) | Various `.razor` files per folder |
| `src/WebApp/Components/Layout` | Shared layout components (MainLayout, HeaderBar, CartMenu, FooterBar) | `MainLayout.razor` |
| `src/WebApp/Components/Chatbot` | AI chatbot UI component and state management | `Chatbot.razor`, `ChatState.cs`, `MessageProcessor.cs` |
| `src/WebApp/Services` | Business logic and integration services (basket, catalog, ordering, authentication) | `BasketService.cs`, `BasketState.cs`, `OrderingService.cs` |
| `src/WebApp/Services/OrderStatus` | Order status notification orchestration and integration event handling | `OrderStatusNotificationService.cs` |
| `src/WebApp/Services/OrderStatus/IntegrationEvents` | Event definitions and handlers for order lifecycle updates | `Events/`, `EventHandling/` |
| `src/WebApp/Extensions` | Dependency injection and service registration configuration | `Extensions.cs` |
| `src/WebApp/Properties` | Runtime configuration and launch settings | `launchSettings.json` |

---

## Components

COMPONENT_NAME: WebApp
TYPE: API
PURPOSE: Blazor Server interactive web application serving as the primary user interface for eShop e-commerce platform.
RESPONSIBILITIES:
  - Routes incoming HTTP requests to Razor components based on URL
  - Manages component rendering and interactive stateful updates
  - Enforces authentication and authorization policies
  - Serves static assets (CSS, JavaScript, images) from wwwroot
  - Proxies product image requests to Catalog.API
  - Logs unhandled exceptions and HTTP request/response details
SOURCE: `Program.cs`, `Components/App.razor`, `Components/Routes.razor`
CALLS:
  - RazorRouter — to match URL paths to page components
  - AuthenticationMiddleware — to validate user identity on each request
  - StaticFileMiddleware — to serve CSS, JavaScript, and image assets
  - ImageForwarder — to proxy product images from Catalog.API
CALLED_BY:
  - External HTTP clients (browsers)

---

COMPONENT_NAME: RazorRouter
TYPE: Gateway
PURPOSE: Routes HTTP requests to appropriate Razor page components based on URL path and query parameters.
RESPONSIBILITIES:
  - Matches incoming URL routes to page components
  - Maintains focus on navigation for accessibility
  - Renders default layout (MainLayout) for all matched routes
  - Handles 404 not-found scenarios
SOURCE: `Components/Routes.razor`
CALLS:
  - MainLayout — to wrap all matched page components with shared layout
  - Page components (Catalog, Cart, Checkout, Item, User pages) — to render matched route
CALLED_BY:
  - WebApp (HTTP request pipeline)

---

COMPONENT_NAME: MainLayout
TYPE: Gateway
PURPOSE: Provides shared layout structure wrapping all pages with header, footer, and chatbot interface.
RESPONSIBILITIES:
  - Renders HeaderBar component at top of page
  - Renders Body placeholder for page-specific content
  - Renders ShowChatbotButton and Chatbot components for AI assistance
  - Renders FooterBar component at bottom of page
  - Displays error UI overlay on unhandled exceptions
SOURCE: `Components/Layout/MainLayout.razor`
CALLS:
  - HeaderBar — to display navigation and shopping cart button
  - ShowChatbotButton — to show button to toggle chatbot
  - Chatbot — to render AI chatbot interface when enabled
  - FooterBar — to display footer content
CALLED_BY:
  - RazorRouter

---

COMPONENT_NAME: BasketState
TYPE: Service
PURPOSE: Manages shopping basket state in memory and synchronizes with backend Basket.API via gRPC.
RESPONSIBILITIES:
  - Maintains cached collection of basket items for current user session
  - Adds and removes items from basket, updating quantity if product already exists
  - Deletes entire basket for checkout or user request
  - Fetches current basket from Basket.API on demand
  - Notifies subscribed UI components when basket contents change
  - Validates user authentication before exposing basket data
SOURCE: `Services/BasketState.cs`
CALLS:
  - BasketService — to fetch, update, or delete basket via gRPC
  - CatalogService — to retrieve product details for basket items
  - AuthenticationStateProvider — to check if user is authenticated
CALLED_BY:
  - Cart page components
  - Basket-related UI components (CartMenu)
  - BasketService

---

COMPONENT_NAME: BasketService
TYPE: Service
PURPOSE: Provides gRPC client for communication with Basket.API to persist and manage shopping baskets.
RESPONSIBILITIES:
  - Sends GetBasketAsync requests to retrieve current user's basket
  - Sends UpdateBasketAsync requests to update basket item quantities
  - Sends DeleteBasketAsync requests to remove entire basket
  - Maps Basket.API gRPC response types to internal BasketQuantity records
SOURCE: `Services/BasketService.cs`
CALLS:
  - Basket.BasketClient (gRPC) — to execute basket operations at Basket.API
CALLED_BY:
  - BasketState

---

COMPONENT_NAME: CatalogService
TYPE: Service
PURPOSE: Provides HTTP client for communication with Catalog.API to retrieve product and catalog data.
RESPONSIBILITIES:
  - Executes HTTP requests to Catalog.API endpoints to fetch catalog items
  - Retrieves product details by ID
  - Manages API versioning (v2.0) for Catalog endpoint
  - Automatically attaches authentication token to outbound requests
SOURCE: Not directly visible; registered in `Extensions/Extensions.cs` with base address `https+http://catalog-api`
CALLS:
  - Catalog.API (HTTP) — to fetch product and catalog data
CALLED_BY:
  - BasketState — to enrich basket items with product details
  - Catalog page components
  - Chatbot component — to retrieve product information for assistance

---

COMPONENT_NAME: OrderingService
TYPE: Service
PURPOSE: Provides HTTP client for communication with Ordering.API to retrieve and create orders.
RESPONSIBILITIES:
  - Sends GetOrders() HTTP requests to retrieve user's order history
  - Sends CreateOrder() HTTP requests to place new orders with idempotency tokens
  - Automatically attaches authentication token to outbound requests
  - Manages API versioning (v1.0) for Ordering endpoint
SOURCE: `Services/OrderingService.cs`
CALLS:
  - Ordering.API (HTTP) — to fetch orders and create new orders
CALLED_BY:
  - Order page components
  - Checkout workflow components

---

COMPONENT_NAME: OrderStatusNotificationService
TYPE: Service
PURPOSE: Manages in-memory subscriptions for order status change notifications, coordinating updates across UI components.
RESPONSIBILITIES:
  - Accepts subscriptions from UI components with buyer ID and callback function
  - Maintains dictionary of subscriptions grouped by buyer ID
  - Triggers all callbacks for a buyer when order status changes
  - Automatically unsubscribes and cleans up subscriptions when disposed
  - Uses lock-based synchronization for thread-safe subscription management
SOURCE: `Services/OrderStatus/OrderStatusNotificationService.cs`
CALLS:
  - NONE
CALLED_BY:
  - OrderStatus integration event handlers — to notify UI of status changes
  - Order history UI components — to subscribe for updates

---

COMPONENT_NAME: OrderStatusChangedToAwaitingValidationIntegrationEventHandler
TYPE: Consumer
PURPOSE: Processes order status change events from event bus and notifies WebApp UI subscribers of validation awaiting status.
RESPONSIBILITIES:
  - Receives OrderStatusChangedToAwaitingValidationIntegrationEvent from RabbitMQ event bus
  - Extracts buyer identity from event payload
  - Delegates notification to OrderStatusNotificationService
  - Logs event receipt and handling for observability
SOURCE: `Services/OrderStatus/IntegrationEvents/EventHandling/OrderStatusChangedToAwaitingValidationIntegrationEventHandler.cs`
CALLS:
  - OrderStatusNotificationService — to notify UI subscribers of status change
CALLED_BY:
  - EventBus (RabbitMQ consumer)

---

COMPONENT_NAME: OrderStatusChangedToPaidIntegrationEventHandler
TYPE: Consumer
PURPOSE: Processes order status change events from event bus and notifies WebApp UI subscribers of payment received status.
RESPONSIBILITIES:
  - Receives OrderStatusChangedToPaidIntegrationEvent from RabbitMQ event bus
  - Extracts buyer identity from event payload
  - Delegates notification to OrderStatusNotificationService
  - Logs event receipt and handling for observability
SOURCE: `Services/OrderStatus/IntegrationEvents/EventHandling/OrderStatusChangedToPaidIntegrationEventHandler.cs`
CALLS:
  - OrderStatusNotificationService — to notify UI subscribers of status change
CALLED_BY:
  - EventBus (RabbitMQ consumer)

---

COMPONENT_NAME: OrderStatusChangedToStockConfirmedIntegrationEventHandler
TYPE: Consumer
PURPOSE: Processes order status change events from event bus and notifies WebApp UI subscribers of stock confirmation status.
RESPONSIBILITIES:
  - Receives OrderStatusChangedToStockConfirmedIntegrationEvent from RabbitMQ event bus
  - Extracts buyer identity from event payload
  - Delegates notification to OrderStatusNotificationService
  - Logs event receipt and handling for observability
SOURCE: `Services/OrderStatus/IntegrationEvents/EventHandling/OrderStatusChangedToStockConfirmedIntegrationEventHandler.cs`
CALLS:
  - OrderStatusNotificationService — to notify UI subscribers of status change
CALLED_BY:
  - EventBus (RabbitMQ consumer)

---

COMPONENT_NAME: OrderStatusChangedToShippedIntegrationEventHandler
TYPE: Consumer
PURPOSE: Processes order status change events from event bus and notifies WebApp UI subscribers of shipment status.
RESPONSIBILITIES:
  - Receives OrderStatusChangedToShippedIntegrationEvent from RabbitMQ event bus
  - Extracts buyer identity from event payload
  - Delegates notification to OrderStatusNotificationService
  - Logs event receipt and handling for observability
SOURCE: `Services/OrderStatus/IntegrationEvents/EventHandling/OrderStatusChangedToShippedIntegrationEventHandler.cs`
CALLS:
  - OrderStatusNotificationService — to notify UI subscribers of status change
CALLED_BY:
  - EventBus (RabbitMQ consumer)

---

COMPONENT_NAME: OrderStatusChangedToCancelledIntegrationEventHandler
TYPE: Consumer
PURPOSE: Processes order cancellation events from event bus and notifies WebApp UI subscribers of cancellation status.
RESPONSIBILITIES:
  - Receives OrderStatusChangedToCancelledIntegrationEvent from RabbitMQ event bus
  - Extracts buyer identity from event payload
  - Delegates notification to OrderStatusNotificationService
  - Logs event receipt and handling for observability
SOURCE: `Services/OrderStatus/IntegrationEvents/EventHandling/OrderStatusChangedToCancelledIntegrationEventHandler.cs`
CALLS:
  - OrderStatusNotificationService — to notify UI subscribers of status change
CALLED_BY:
  - EventBus (RabbitMQ consumer)

---

COMPONENT_NAME: OrderStatusChangedToSubmittedIntegrationEventHandler
TYPE: Consumer
PURPOSE: Processes order submission events from event bus and notifies WebApp UI subscribers of submission status.
RESPONSIBILITIES:
  - Receives OrderStatusChangedToSubmittedIntegrationEvent from RabbitMQ event bus
  - Extracts buyer identity from event payload
  - Delegates notification to OrderStatusNotificationService
  - Logs event receipt and handling for observability
SOURCE: `Services/OrderStatus/IntegrationEvents/EventHandling/OrderStatusChangedToSubmittedIntegrationEventHandler.cs`
CALLS:
  - OrderStatusNotificationService — to notify UI subscribers of status change
CALLED_BY:
  - EventBus (RabbitMQ consumer)

---

COMPONENT_NAME: Chatbot
TYPE: Service
PURPOSE: Provides AI-powered conversational assistance to users by leveraging OpenAI or Ollama language models.
RESPONSIBILITIES:
  - Renders floating chat pane UI with message history
  - Initializes ChatState with user context and AI model configuration
  - Sends user messages to AI service and displays responses
  - Provides product information and shopping assistance through ChatState
  - Displays configuration error message if AI services not enabled
  - Manages chat scroll position and DOM element references
SOURCE: `Components/Chatbot/Chatbot.razor`, `Components/Chatbot/ChatState.cs`, `Components/Chatbot/MessageProcessor.cs`
CALLS:
  - IChatClient (OpenAI or Ollama) — to send messages and receive AI responses
  - ChatState — to maintain conversation state and process responses
  - CatalogService — to retrieve product information for AI context
  - BasketState — to provide current basket context to chatbot
  - AuthenticationStateProvider — to obtain current user claims
CALLED_BY:
  - MainLayout

---

COMPONENT_NAME: LogOutService
TYPE: Service
PURPOSE: Handles user session termination and sign-out from both cookie and OpenIdConnect authentication schemes.
RESPONSIBILITIES:
  - Signs out user from cookie-based authentication scheme
  - Signs out user from OpenIdConnect authentication scheme
  - Clears authentication tokens and session state
SOURCE: `Services/LogOutService.cs`
CALLS:
  - NONE (calls ASP.NET Core authentication middleware directly via HttpContext)
CALLED_BY:
  - User menu component — on logout action

---

COMPONENT_NAME: ProductImageUrlProvider
TYPE: Utility
PURPOSE: Generates product image URLs with proper API versioning for catalog product pictures.
RESPONSIBILITIES:
  - Constructs relative image URLs in format `product-images/{productId}?api-version=2.0`
  - Ensures consistent API versioning across image requests
SOURCE: `Services/ProductImageUrlProvider.cs`
CALLS:
  - NONE
CALLED_BY:
  - Page components — to render product images
  - Chatbot component — to format product images in responses

---

COMPONENT_NAME: ImageForwarder
TYPE: Gateway
PURPOSE: HTTP forwarder that proxies product image requests from WebApp to Catalog.API backend.
RESPONSIBILITIES:
  - Intercepts HTTP requests to `/product-images/{id}` route
  - Forwards requests to Catalog.API endpoint `/api/catalog/items/{id}/pic`
  - Handles HTTPS and HTTP protocol variations (`https+http://catalog-api`)
SOURCE: `Program.cs` route mapping: `app.MapForwarder("/product-images/{id}", "https+http://catalog-api", "/api/catalog/items/{id}/pic")`
CALLS:
  - Catalog.API — to fetch product image data
CALLED_BY:
  - WebApp (HTTP pipeline)

---

COMPONENT_NAME: AuthenticationMiddleware
TYPE: Gateway
PURPOSE: Validates user identity and enforces authentication claims required by application pages and services.
RESPONSIBILITIES:
  - Validates cookie and OpenIdConnect authentication tokens on each request
  - Enforces authentication policy on protected routes
  - Provides ClaimsPrincipal context to components and services
  - Redirects unauthenticated users to Identity.API login
SOURCE: `Extensions/Extensions.cs` authentication configuration section
CALLS:
  - Identity.API — to validate tokens and retrieve user claims via OpenIdConnect
CALLED_BY:
  - WebApp (HTTP request pipeline)

---

COMPONENT_NAME: EventBusSubscriber
TYPE: Consumer
PURPOSE: RabbitMQ event bus consumer that subscribes to order status change events and dispatches to appropriate handlers.
RESPONSIBILITIES:
  - Subscribes to 6 order status integration event types from RabbitMQ "Ordering.webapp" consumer group
  - Routes received messages to corresponding event handler classes
  - Handles message acknowledgment and error scenarios
SOURCE: `Extensions/Extensions.cs` event bus subscriptions in `AddEventBusSubscriptions()` method
CALLS:
  - OrderStatusChanged*IntegrationEventHandlers — to process each event type
CALLED_BY:
  - RabbitMQ event bus (message broker)

---

## Component Call Sequences

### Use-Case: Browse Catalog and Add Item to Basket

STEP 1: User navigates to catalog page
  OPERATION: HTTP GET /catalog
  PURPOSE: User requests catalog browsing interface
  SOURCE: `Components/Routes.razor`

STEP 2: Catalog page component → CatalogService.GetCatalogItems()
  OPERATION: HTTP GET https+http://catalog-api/api/v2.0/items
  PURPOSE: Fetch product list and display in UI
  SOURCE: Catalog page component (not shown; inferred from CatalogService registration)

STEP 3: Catalog page component → BasketState.GetBasketItemsAsync()
  OPERATION: Retrieve current user's basket items
  PURPOSE: Determine which products are already in cart and display status
  SOURCE: `Services/BasketState.cs` GetBasketItemsAsync() method

STEP 4: BasketState → AuthenticationStateProvider.GetAuthenticationStateAsync()
  OPERATION: Retrieve current user's claims
  PURPOSE: Check if user is authenticated before fetching basket
  SOURCE: `Services/BasketState.cs` GetBasketItemsAsync() method

STEP 5: BasketState → BasketService.GetBasketAsync()
  OPERATION: gRPC call to Basket.BasketClient.GetBasketAsync()
  PURPOSE: Retrieve user's current basket contents from Basket.API
  SOURCE: `Services/BasketState.cs` FetchBasketItemsAsync() method

STEP 6: BasketState → CatalogService.GetProductDetails()
  OPERATION: HTTP GET retrieve product metadata for basket items
  PURPOSE: Enrich basket display with product names, prices, images
  SOURCE: `Services/BasketState.cs` basket enrichment logic

STEP 7: User clicks "Add to Basket" on catalog item
  OPERATION: Catalog page → BasketState.AddAsync(catalogItem)
  PURPOSE: Add selected product to shopping basket
  SOURCE: Catalog page component action handler

STEP 8: BasketState.AddAsync() → BasketService.UpdateBasketAsync()
  OPERATION: gRPC call to Basket.BasketClient.UpdateBasketAsync()
  PURPOSE: Persist updated basket with new item quantity to Basket.API
  SOURCE: `Services/BasketState.cs` AddAsync() method

STEP 9: BasketState → NotifyOnChange subscribers
  OPERATION: Invoke registered event callbacks
  PURPOSE: Update all subscribed UI components (cart display, cart count badge) with new basket state
  SOURCE: `Services/BasketState.cs` NotifyOnChange() method

---

### Use-Case: Place Order and Receive Status Updates

STEP 1: User navigates to checkout page
  OPERATION: HTTP GET /checkout
  PURPOSE: User requests order placement interface
  SOURCE: `Components/Routes.razor`

STEP 2: Checkout page component → BasketState.GetBasketItemsAsync()
  OPERATION: Retrieve current user's basket items
  PURPOSE: Display items to be ordered for confirmation
  SOURCE: Checkout page component

STEP 3: User submits order form with delivery and payment details
  OPERATION: Checkout page → OrderingService.CreateOrder(request, requestId)
  PURPOSE: Send order details to backend for processing
  SOURCE: `Services/OrderingService.cs` CreateOrder() method

STEP 4: OrderingService → Ordering.API
  OPERATION: HTTP POST https+http://ordering-api/api/Orders/ with x-requestid header
  PURPOSE: Backend creates order, initiates validation, payment processing, fulfillment workflows
  SOURCE: `Services/OrderingService.cs` CreateOrder() method

STEP 5: Ordering.API publishes OrderStatusChangedToSubmittedIntegrationEvent
  OPERATION: Event published to RabbitMQ "Ordering.webapp" topic
  PURPOSE: Notify consumers that order was submitted
  SOURCE: Ordering.API (external service)

STEP 6: EventBusSubscriber receives OrderStatusChangedToSubmittedIntegrationEvent
  OPERATION: RabbitMQ message routed to OrderStatusChangedToSubmittedIntegrationEventHandler
  PURPOSE: Process order status change notification
  SOURCE: `Services/OrderStatus/IntegrationEvents/EventHandling/OrderStatusChangedToSubmittedIntegrationEventHandler.cs`

STEP 7: OrderStatusChangedToSubmittedIntegrationEventHandler → OrderStatusNotificationService.NotifyOrderStatusChangedAsync()
  OPERATION: Trigger all registered callbacks for buyer
  PURPOSE: Notify all UI components subscribed to this order's status changes
  SOURCE: `Services/OrderStatus/OrderStatusNotificationService.cs` NotifyOrderStatusChangedAsync() method

STEP 8: OrderStatusNotificationService invokes UI subscription callbacks
  OPERATION: Each subscribed component callback is awaited and executed
  PURPOSE: Update order history UI and order status display with new status
  SOURCE: Order history/status page component subscriptions

STEP 9: Ordering.API publishes subsequent order status events (AwaitingValidation, Paid, StockConfirmed, Shipped, Cancelled)
  OPERATION: Multiple events published to RabbitMQ "Ordering.webapp" over time
  PURPOSE: Notify of order progression through fulfillment pipeline
  SOURCE: Ordering.API (external service)

STEP 10: EventBusSubscriber processes each status event with corresponding handler
  OPERATION: New event received → Handler processes → OrderStatusNotificationService notifies UI
  PURPOSE: UI components receive real-time order status updates without polling
  SOURCE: Similar flow as steps 6-8 for each event type

---

## Communication Channels

CHANNEL_TYPE: gRPC
ENDPOINT: http://basket-api (gRPC endpoint)
SOURCE: `Extensions/Extensions.cs` line: `.AddGrpcClient<Basket.BasketClient>(o => o.Address = new("http://basket-api"))`
NOTES: gRPC client factory injected into BasketService; uses protocol buffers from `Proto/basket.proto`; authentication token attached via AddAuthToken()

---

CHANNEL_TYPE: HTTP
ENDPOINT: https+http://catalog-api (HTTP with fallback to insecure variant)
SOURCE: `Extensions/Extensions.cs` line: `.AddHttpClient<CatalogService>(o => o.BaseAddress = new("https+http://catalog-api"))`
NOTES: HTTP client for Catalog.API v2.0; uses service discovery via Yarp; API version header added; authentication token attached

---

CHANNEL_TYPE: HTTP
ENDPOINT: https+http://ordering-api (HTTP with fallback to insecure variant)
SOURCE: `Extensions/Extensions.cs` line: `.AddHttpClient<OrderingService>(o => o.BaseAddress = new("https+http://ordering-api"))`
NOTES: HTTP client for Ordering.API v1.0; uses service discovery via Yarp; x-requestid idempotency key included in CreateOrder calls; authentication token attached

---

CHANNEL_TYPE: MessageQueue
ENDPOINT: RabbitMQ consumer group "Ordering.webapp"
SOURCE: `Extensions/Extensions.cs` line: `.AddRabbitMqEventBus("EventBus").AddEventBusSubscriptions()`
NOTES: Subscribes to 6 order status integration event types; configured via EventBus NuGet package; consumer automatically acknowledges messages on successful handler completion

---

CHANNEL_TYPE: HTTP (forwarded)
ENDPOINT: /product-images/{id} (local) → https+http://catalog-api/api/catalog/items/{id}/pic (remote)
SOURCE: `Program.cs` line: `app.MapForwarder("/product-images/{id}", "https+http://catalog-api", "/api/catalog/items/{id}/pic")`
NOTES: Built-in Yarp HTTP forwarder; proxies product image requests transparently; supports API versioning parameter

---

CHANNEL_TYPE: HTTP (OpenIdConnect)
ENDPOINT: {IdentityUrl}/.well-known/openid-configuration (configurable via config)
SOURCE: `Extensions/Extensions.cs` Authentication configuration: `options.Authority = identityUrl;`
NOTES: Identity server endpoint for OpenIdConnect discovery and token validation; redirects unauthenticated requests to login; requires IdentityUrl environment/config variable

---

## Dependency Registration and Wiring

DI_CONTAINER: Built-in ASP.NET Core Dependency Injection (Microsoft.Extensions.DependencyInjection)

REGISTRATION_FILE: `Extensions/Extensions.cs` method `AddApplicationServices()`

| Lifetime | Abstraction | Concrete Implementation | Source Line |
|----------|-------------|------------------------|------------|
| Scoped | BasketState | BasketState | `builder.Services.AddScoped<BasketState>();` |
| Scoped | LogOutService | LogOutService | `builder.Services.AddScoped<LogOutService>();` |
| Singleton | BasketService | BasketService | `builder.Services.AddSingleton<BasketService>();` |
| Singleton | OrderStatusNotificationService | OrderStatusNotificationService | `builder.Services.AddSingleton<OrderStatusNotificationService>();` |
| Singleton | IProductImageUrlProvider | ProductImageUrlProvider | `builder.Services.AddSingleton<IProductImageUrlProvider, ProductImageUrlProvider>();` |
| Factory | Basket.BasketClient (gRPC) | gRPC client factory | `builder.Services.AddGrpcClient<Basket.BasketClient>(...)` |
| Factory | CatalogService (HttpClient) | HTTP client factory | `builder.Services.AddHttpClient<CatalogService>(...)` |
| Factory | OrderingService (HttpClient) | HTTP client factory | `builder.Services.AddHttpClient<OrderingService>(...)` |
| Singleton | AuthenticationStateProvider | ServerAuthenticationStateProvider | `services.AddScoped<AuthenticationStateProvider, ServerAuthenticationStateProvider>();` |

**Event Bus Subscriptions** (registered via `AddEventBusSubscriptions()` method):

```csharp
eventBus.AddSubscription<
  OrderStatusChangedToAwaitingValidationIntegrationEvent,
  OrderStatusChangedToAwaitingValidationIntegrationEventHandler>();
eventBus.AddSubscription<
  OrderStatusChangedToPaidIntegrationEvent,
  OrderStatusChangedToPaidIntegrationEventHandler>();
eventBus.AddSubscription<
  OrderStatusChangedToStockConfirmedIntegrationEvent,
  OrderStatusChangedToStockConfirmedIntegrationEventHandler>();
eventBus.AddSubscription<
  OrderStatusChangedToShippedIntegrationEvent,
  OrderStatusChangedToShippedIntegrationEventHandler>();
eventBus.AddSubscription<
  OrderStatusChangedToCancelledIntegrationEvent,
  OrderStatusChangedToCancelledIntegrationEventHandler>();
eventBus.AddSubscription<
  OrderStatusChangedToSubmittedIntegrationEvent,
  OrderStatusChangedToSubmittedIntegrationEventHandler>();
```

---

## Configuration and Secrets

SOURCE_TYPE: Configuration file
KEYS: SessionCookieLifetimeMinutes, IdentityUrl, CallBackUrl, OllamaEnabled, OllamaEndpoint, UseOpenAI (inferred)
SENSITIVE: YES (contains identity server URLs and client credentials)
LOCATION: `appsettings.json` (base), `appsettings.Development.json` (development overrides)

---

SOURCE_TYPE: Environment variable or configuration
KEYS: IdentityUrl, CallBackUrl
SENSITIVE: YES (identity server endpoint URLs)
LOCATION: Configuration from `IConfiguration` in `AddAuthenticationServices()`; source depends on hosting environment

---

SOURCE_TYPE: Secrets management
KEYS: ClientSecret (hardcoded "secret" in OpenIdConnect options)
SENSITIVE: YES
LOCATION: `Extensions/Extensions.cs` line: `options.ClientSecret = "secret";` (SHOULD be externalized to user secrets in development or Key Vault in production)

---

SOURCE_TYPE: Configuration
KEYS: OllamaEnabled, OpenAI configuration
SENSITIVE: NO (feature flags), YES (API keys if configured)
LOCATION: `Extensions/Extensions.cs` AI services configuration section; conditional based on OllamaEnabled flag

---

## Persistence and Data Access

DATABASE: NONE (WebApp is stateless frontend; all data access delegated to backend services)
DATA_ACCESS: N/A — Data access layer implemented in backend services (Basket.API, Catalog.API, Ordering.API)
MIGRATIONS_PATH: NONE
REPOSITORY_PATTERN: NO (WebApp uses service-based architecture with HTTP/gRPC clients rather than repositories)

**In-Memory State Management:**
- BasketState maintains transient in-memory cache of basket items per session
- BasketState cache is session-scoped and regenerated on each page load or explicit refresh
- Order status subscriptions stored in OrderStatusNotificationService singleton dictionary keyed by buyerId

---

## Patterns and Architecture Notes

PATTERN: Blazor Server Interactive Rendering
EVIDENCE: `Program.cs`: `builder.Services.AddRazorComponents().AddInteractiveServerComponents();`
EVIDENCE: `Components/Routes.razor`: `.AddInteractiveServerRenderMode();`
SNIPPET: WebApp uses interactive server-side rendering to enable real-time UI updates and server-maintained state without full-page refreshes

---

PATTERN: Event-Driven Architecture (Consumer)
EVIDENCE: `Services/OrderStatus/IntegrationEvents/EventHandling/` — 6 event handler classes implementing IIntegrationEventHandler
EVIDENCE: `Extensions/Extensions.cs` — RabbitMQ event bus subscriptions registered in `AddEventBusSubscriptions()`
SNIPPET: WebApp subscribes to order status change events published by Ordering.API, enabling asynchronous real-time updates to order history UI without polling

---

PATTERN: Service Locator (Client-Side Service Integration)
EVIDENCE: `Extensions/Extensions.cs` — Centralized service registration for gRPC (Basket), HTTP (Catalog, Ordering), and AI clients
EVIDENCE: `BasketState`, `OrderingService`, `CatalogService` — Injected via constructor dependency injection
SNIPPET: Multiple backend services registered as typed HTTP/gRPC clients with automatic token attachment and service discovery via Yarp

---

PATTERN: State Management via Observable Pattern
EVIDENCE: `Services/BasketState.cs` — `NotifyOnChange()` method returns IDisposable subscription
EVIDENCE: `Services/OrderStatus/OrderStatusNotificationService.cs` — In-memory subscription registry with thread-safe lock-based access
SNIPPET: UI components subscribe to basket and order status changes; notifications trigger via async callbacks when state transitions occur

---

PATTERN: In-Memory Observable Caching
EVIDENCE: `Services/BasketState.cs` — `_cachedBasket` field caches GetBasketItemsAsync result; `_changeSubscriptions` maintains active subscribers
EVIDENCE: `Services/OrderStatus/OrderStatusNotificationService.cs` — `_subscriptionsByBuyerId` maintains buyer-keyed subscriptions
SNIPPET: Reduces repeated backend calls by maintaining session-local cache; invalidation managed via event notification callbacks

---

PATTERN: Authentication Delegation (OpenIdConnect with Cookie)
EVIDENCE: `Extensions/Extensions.cs` — Dual scheme configuration combining OpenIdConnect (redirects, token validation) and Cookie (session persistence)
EVIDENCE: `Services/LogOutService.cs` — Signs out from both schemes on user logout
SNIPPET: Uses OpenIdConnect for identity provider integration and cookie for stateful session management; requires Identity.API backend

---

PATTERN: API Versioning via HTTP Headers
EVIDENCE: `Extensions/Extensions.cs` — `.AddApiVersion(2.0)` and `.AddApiVersion(1.0)` chained on HTTP client registrations
EVIDENCE: `Services/ProductImageUrlProvider.cs` — Appends `?api-version=2.0` to product image URLs
SNIPPET: Maintains backward compatibility by explicitly specifying API version on outbound requests to backend services

---

PATTERN: Razor Component Composition
EVIDENCE: `Components/Layout/MainLayout.razor` — Hierarchical component tree: MainLayout contains HeaderBar, Body, ShowChatbotButton, Chatbot, FooterBar
EVIDENCE: `Components/Routes.razor` — Routes component wraps page content with MainLayout
SNIPPET: Modular component hierarchy enables content slot-based layout where MainLayout remains consistent across all pages

---

## Security and Operational Considerations

AUTHN_AUTHZ: OpenIdConnect + Cookie authentication with token validation
LOCATION: `Extensions/Extensions.cs` — `AddAuthenticationServices()` method configures dual schemes
TOKENS: Bearer tokens obtained from Identity.API via OpenIdConnect code flow; stored in secure HTTP-only cookies
REQUEST_ATTACHMENT: Authentication tokens automatically attached to gRPC and HTTP backend requests via `.AddAuthToken()` extension method

---

KNOWN_RISKS:
- **Hardcoded Client Secret**: OpenIdConnect ClientSecret hardcoded as "secret" in Extensions.cs — should be externalized to user secrets (development) or Key Vault (production)
- **Insecure Protocol Fallback**: Service addresses use `https+http://` which allows downgrades to unencrypted HTTP — production should enforce HTTPS only
- **Session Cookie Vulnerability**: Default session cookie lifetime 60 minutes may be excessive for sensitive operations; expiry time configurable but not enforced by policy
- **Missing CSRF Protection**: Verify that AntiforgeryMiddleware is properly configured for form submissions (partially visible in Program.cs)
- **Chat History Storage**: Chatbot maintains full message history in memory; sensitive user queries may persist in UI state

---

OBSERVABILITY:
- **Logging Framework**: Microsoft.Extensions.Logging (ASP.NET Core built-in)
- **Logging Configuration**: `appsettings.json` sets default log level to Information; ASP.NET Core framework warnings suppressed
- **Event Handler Logging**: `ILogger<OrderStatusChanged*IntegrationEventHandler>` injected into each event handler; logs event receipt and handler execution
- **Health Checks**: `MapDefaultEndpoints()` called in Program.cs; inherited from ServiceDefaults may expose /health endpoint

---

DEPLOYMENT:
- **Docker Support**: Project is containerized; WebApp.csproj specifies `net10.0` target framework
- **Configuration**: Targets .NET 10.0 Web SDK; depends on eShop.ServiceDefaults for observability and shared configuration
- **Environment-Specific Config**: appsettings.Development.json for local development; appsettings.json for production defaults
- **Service Discovery**: Uses YARP (Reverse Proxy) for HTTP service discovery to Catalog, Ordering, and Basket APIs; hostname resolution via container networking
- **gRPC Proto Files**: Generates C# client from `../Basket.API/Proto/basket.proto` at build time

---
