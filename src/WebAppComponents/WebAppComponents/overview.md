# WebAppComponents

A Razor Component Library providing reusable UI components and services for catalog browsing, product search filtering, and image URL resolution in web applications.

## Summary

- Shared Razor Class Library (.NET 10.0) offering reusable UI components for product catalog functionality
- Provides components: CatalogListItem (product card display), CatalogSearch (filter by brand/type)
- Exposes service interfaces: ICatalogService (HTTP client to remote catalog API), IProductImageUrlProvider (image URL abstraction)
- Consumes remote Catalog API and integrates seamlessly with hosting web applications via dependency injection
- No persistence layer; all data sourced from external Catalog.API via HTTP

## Projects and Folder Map

PATH: `src/WebAppComponents/`
PURPOSE: Root of Razor Component Library
ENTRY_FILES: `_Imports.razor`, `WebAppComponents.csproj`

---

PATH: `src/WebAppComponents/Catalog/`
PURPOSE: Razor components and models for product catalog display and filtering
ENTRY_FILES: `CatalogListItem.razor`, `CatalogSearch.razor`, `CatalogItem.cs`

---

PATH: `src/WebAppComponents/Services/`
PURPOSE: Service interfaces and HTTP client implementations for catalog operations and image URL resolution
ENTRY_FILES: `ICatalogService.cs`, `CatalogService.cs`, `IProductImageUrlProvider.cs`

---

PATH: `src/WebAppComponents/Item/`
PURPOSE: Utility module for item-related operations (URL generation)
ENTRY_FILES: `ItemHelper.cs`

---

## Components

COMPONENT_NAME: CatalogListItem
TYPE: Utility
PURPOSE: Razor component that renders a single product card displaying item name, price, and image.
RESPONSIBILITIES:
  - Accepts a CatalogItem parameter and IsLoggedIn flag
  - Renders product image via IProductImageUrlProvider
  - Generates navigable link to product detail page using ItemHelper.Url()
  - Displays product name and formatted price
SOURCE: `src/WebAppComponents/Catalog/CatalogListItem.razor`
CALLS:
  - IProductImageUrlProvider — to resolve the URL for the product image
  - ItemHelper — to generate the item detail page URL
CALLED_BY:
  - Catalog.razor (from WebApp) — consumed in product list rendering loops

---

COMPONENT_NAME: CatalogSearch
TYPE: Utility
PURPOSE: Razor component that renders a filter panel allowing users to filter products by brand and type.
RESPONSIBILITIES:
  - Injects CatalogService and NavigationManager
  - Loads brands and types on component initialization
  - Renders clickable filter tags for each brand and type
  - Generates query parameter URIs for filter state preservation
SOURCE: `src/WebAppComponents/Catalog/CatalogSearch.razor`
CALLS:
  - CatalogService — to fetch available brands (GetBrands)
  - CatalogService — to fetch available types (GetTypes)
  - NavigationManager — to build URIs with query parameters for filter persistence
CALLED_BY:
  - Catalog.razor (from WebApp) — rendered alongside product list

---

COMPONENT_NAME: CatalogService
TYPE: Service
PURPOSE: Provides HTTP client implementation for communicating with remote Catalog.API to retrieve product data.
RESPONSIBILITIES:
  - Executes GET requests to remote `api/catalog/` endpoints
  - Constructs query strings for pagination, filtering (brand, type), and semantic search
  - Deserializes JSON responses to CatalogItem, CatalogResult, CatalogBrand, and CatalogItemType records
  - Supports retrieval of single items, paginated lists, items by ID collection, and semantic search
SOURCE: `src/WebAppComponents/Services/CatalogService.cs`
CALLS:
  - HttpClient — to issue GET requests to Catalog.API endpoints
CALLED_BY:
  - CatalogSearch — to retrieve brands and types for filter UI
  - WebApp components (via injected dependency) — to retrieve product data for display

---

COMPONENT_NAME: ItemHelper
TYPE: Utility
PURPOSE: Provides static helper methods for item-related URL generation.
RESPONSIBILITIES:
  - Generates relative URL paths for product detail pages
SOURCE: `src/WebAppComponents/Item/ItemHelper.cs`
CALLS:
  - NONE
CALLED_BY:
  - CatalogListItem — to create navigable links to product detail pages

---

COMPONENT_NAME: CatalogItem
TYPE: Utility
PURPOSE: Represents a product entity with brand and type relationships; used across catalog operations.
RESPONSIBILITIES:
  - Defines product data structure (Id, Name, Description, Price, PictureUrl, CatalogBrandId, CatalogTypeId)
  - Provides data container for brand and type metadata
SOURCE: `src/WebAppComponents/Catalog/CatalogItem.cs`
CALLS:
  - NONE
CALLED_BY:
  - CatalogListItem — receives as parameter
  - CatalogService — returns in query results
  - ItemHelper — queries Id property

---

COMPONENT_NAME: CatalogResult
TYPE: Utility
PURPOSE: Wraps paginated catalog search results with metadata.
RESPONSIBILITIES:
  - Containers for paginated product lists with page index, size, and total count
SOURCE: `src/WebAppComponents/Catalog/CatalogItem.cs`
CALLS:
  - NONE
CALLED_BY:
  - CatalogService — returned from paginated query methods

---

COMPONENT_NAME: IProductImageUrlProvider
TYPE: Service
PURPOSE: Abstraction for resolving product image URLs, allowing consuming applications to implement custom URL logic.
RESPONSIBILITIES:
  - Defines interface for image URL resolution by product entity or ID
  - Provides default implementation using product ID
SOURCE: `src/WebAppComponents/Services/IProductImageUrlProvider.cs`
CALLS:
  - NONE
CALLED_BY:
  - CatalogListItem — injected and called to resolve image URL

---

## Component Call Sequences

### Use-Case: Browse Catalog with Filters

STEP 1: WebApp (Catalog.razor) → CatalogSearch
  OPERATION: Initial component render
  PURPOSE: Display filter panel on catalog page
  SOURCE: `src/WebApp/Components/Pages/Catalog/Catalog.razor`

STEP 2: CatalogSearch → CatalogService.GetBrands()
  OPERATION: OnInitializedAsync lifecycle
  PURPOSE: Fetch list of available product brands to populate filter UI
  SOURCE: `src/WebAppComponents/Catalog/CatalogSearch.razor`, `src/WebAppComponents/Services/CatalogService.cs`

STEP 3: CatalogSearch → CatalogService.GetTypes()
  OPERATION: OnInitializedAsync lifecycle (parallel with Step 2)
  PURPOSE: Fetch list of available product types to populate filter UI
  SOURCE: `src/WebAppComponents/Catalog/CatalogSearch.razor`, `src/WebAppComponents/Services/CatalogService.cs`

STEP 4: CatalogService → Catalog.API
  OPERATION: GET api/catalog/catalogBrands
  PURPOSE: Retrieve all available brands from remote API
  SOURCE: `src/WebAppComponents/Services/CatalogService.cs`

STEP 5: CatalogService → Catalog.API
  OPERATION: GET api/catalog/catalogTypes
  PURPOSE: Retrieve all available types from remote API
  SOURCE: `src/WebAppComponents/Services/CatalogService.cs`

STEP 6: WebApp (Catalog.razor) → CatalogListItem
  OPERATION: Component render in loop for each product
  PURPOSE: Render product card for each item in current result set
  SOURCE: `src/WebApp/Components/Pages/Catalog/Catalog.razor`

STEP 7: CatalogListItem → IProductImageUrlProvider.GetProductImageUrl()
  OPERATION: Component render
  PURPOSE: Resolve the URL for product image to display
  SOURCE: `src/WebAppComponents/Catalog/CatalogListItem.razor`

STEP 8: CatalogListItem → ItemHelper.Url()
  OPERATION: Component render
  PURPOSE: Generate navigable URL to product detail page
  SOURCE: `src/WebAppComponents/Catalog/CatalogListItem.razor`

---

### Use-Case: View Product Details

STEP 1: WebApp (ItemPage.razor) → IProductImageUrlProvider.GetProductImageUrl()
  OPERATION: Component parameter binding
  PURPOSE: Resolve product image URL for detail page display
  SOURCE: `src/WebApp/Components/Pages/Item/ItemPage.razor`

STEP 2: IProductImageUrlProvider → WebApp ProductImageUrlProvider (implementation)
  OPERATION: GetProductImageUrl(productId)
  PURPOSE: Provide concrete URL format for product image (e.g., `/product-images/{id}`)
  SOURCE: `src/WebApp/Services/ProductImageUrlProvider.cs`

STEP 3: WebApp (ItemPage) → CatalogService.GetCatalogItem()
  OPERATION: Page initialization
  PURPOSE: Fetch complete product details including description, brand, and type information
  SOURCE: `src/WebApp/Components/Pages/Item/ItemPage.razor`

STEP 4: CatalogService → Catalog.API
  OPERATION: GET api/catalog/items/{id}
  PURPOSE: Retrieve individual product details from remote API
  SOURCE: `src/WebAppComponents/Services/CatalogService.cs`

---

## Communication Channels

CHANNEL_TYPE: HTTP
ENDPOINT: `api/catalog/items/{id}`
SOURCE: `src/WebAppComponents/Services/CatalogService.cs`
NOTES: GET request; returns single CatalogItem; called by GetCatalogItem()

---

CHANNEL_TYPE: HTTP
ENDPOINT: `api/catalog/items`
SOURCE: `src/WebAppComponents/Services/CatalogService.cs`
NOTES: GET request with query parameters (pageIndex, pageSize, brand, type); returns CatalogResult; called by GetCatalogItems()

---

CHANNEL_TYPE: HTTP
ENDPOINT: `api/catalog/items/by`
SOURCE: `src/WebAppComponents/Services/CatalogService.cs`
NOTES: GET request with ids query parameter; returns List<CatalogItem>; called by GetCatalogItems(IEnumerable<int>)

---

CHANNEL_TYPE: HTTP
ENDPOINT: `api/catalog/items/withsemanticrelevance`
SOURCE: `src/WebAppComponents/Services/CatalogService.cs`
NOTES: GET request with text, pageIndex, pageSize parameters; returns CatalogResult; called by GetCatalogItemsWithSemanticRelevance()

---

CHANNEL_TYPE: HTTP
ENDPOINT: `api/catalog/catalogBrands`
SOURCE: `src/WebAppComponents/Services/CatalogService.cs`
NOTES: GET request; returns CatalogBrand[]; called by GetBrands()

---

CHANNEL_TYPE: HTTP
ENDPOINT: `api/catalog/catalogTypes`
SOURCE: `src/WebAppComponents/Services/CatalogService.cs`
NOTES: GET request; returns CatalogItemType[]; called by GetTypes()

---

## Dependency Registration and Wiring

DI_CONTAINER: Microsoft.AspNetCore.Components.Web (Blazor built-in DI)

REGISTRATION_FILE: `src/WebApp/Extensions/Extensions.cs` (method AddApplicationServices)

---

SERVICE_REGISTRATION: CatalogService
TYPE: Named (HttpClient)
LIFETIME: Registered as part of HttpClientFactory
SOURCE: `src/WebApp/Extensions/Extensions.cs`
SNIPPET:
```csharp
builder.Services.AddHttpClient<CatalogService>();
```
NOTES: CatalogService relies on dependency injection of HttpClient; configured at WebApp level, not in WebAppComponents

---

SERVICE_REGISTRATION: IProductImageUrlProvider
IMPLEMENTATION: ProductImageUrlProvider
LIFETIME: Singleton
SOURCE: `src/WebApp/Extensions/Extensions.cs`, line 27
SNIPPET:
```csharp
builder.Services.AddSingleton<IProductImageUrlProvider, ProductImageUrlProvider>();
```
NOTES: Interface defined in WebAppComponents; concrete implementation provided by WebApp; registered as singleton for performance

---

SERVICE_REGISTRATION: Component Discovery
TYPE: Razor Component Library
LIFETIME: Compile-time
SOURCE: `WebAppComponents.csproj`
NOTES: All Razor components automatically discovered via RootNamespace; imported in consuming applications via `@using` directives in _Imports.razor

---

## Configuration and Secrets

SOURCE_TYPE: application configuration (runtime)
KEYS: Remote service base URL for Catalog API
SENSITIVE: NO
LOCATION: `src/WebAppComponents/Services/CatalogService.cs` (hardcoded as `"api/catalog/"`)
NOTES: Relative URL; relies on HttpClient to resolve against configured base address or web app host

---

SOURCE_TYPE: environment configuration
KEYS: HttpClient base address for Catalog API communication
SENSITIVE: NO
LOCATION: `src/WebApp/Program.cs` (HttpClient configuration)
NOTES: Configured at WebApp level; controls remote service endpoint for CatalogService

---

## Persistence and Data Access

DATABASE: NONE
NOTES: WebAppComponents is a presentation layer library with no direct database access.

DATA_ACCESS: HTTP Client (remote API consumption)
DRIVER: System.Net.Http.Json
LOCATION: `src/WebAppComponents/Services/CatalogService.cs`
NOTES: Uses GetFromJsonAsync<T>() to deserialize remote API responses; no ORM or query builder

REPOSITORY_PATTERN: NO
NOTES: CatalogService acts as HTTP client facade, not a repository; actual data persistence handled by remote Catalog.API

---

## Patterns and Architecture Notes

PATTERN: Service Locator / HTTP Client Facade
EVIDENCE: `src/WebAppComponents/Services/CatalogService.cs`
NOTES: CatalogService encapsulates all HTTP communication with the remote Catalog.API, providing a clean interface to consumers

---

PATTERN: Dependency Injection / Inversion of Control
EVIDENCE: `src/WebAppComponents/Catalog/CatalogListItem.razor` (ServiceCollection injection), `src/WebAppComponents/Catalog/CatalogSearch.razor` (CatalogService and NavigationManager injection)
SNIPPET:
```razor
@inject IProductImageUrlProvider ProductImages
@inject CatalogService CatalogService
@inject NavigationManager Nav
```
NOTES: Razor components use property injection for services; enables testability and loose coupling

---

PATTERN: Component Composition
EVIDENCE: `src/WebApp/Components/Pages/Catalog/Catalog.razor`
NOTES: CatalogSearch and CatalogListItem are composed into parent page components; enables reusability across multiple pages

---

PATTERN: Strategy Pattern
EVIDENCE: `src/WebAppComponents/Services/IProductImageUrlProvider.cs` and `src/WebApp/Services/ProductImageUrlProvider.cs`
NOTES: Image URL resolution logic decoupled via interface, allowing consuming applications to implement custom URL strategies

---

PATTERN: Async/Await for Concurrency
EVIDENCE: `src/WebAppComponents/Catalog/CatalogSearch.razor`
SNIPPET:
```csharp
protected override async Task OnInitializedAsync()
{
    var brandsTask = CatalogService.GetBrands();
    var itemTypesTask = CatalogService.GetTypes();
    await Task.WhenAll(brandsTask, itemTypesTask);
```
NOTES: CatalogSearch initializes both brands and types in parallel using Task.WhenAll()

---

## Security and Operational Considerations

AUTHN_AUTHZ: None configured
NOTES: WebAppComponents itself does not enforce authentication/authorization; CatalogService calls remote Catalog.API without explicit auth headers; auth handled by hosting WebApp within IIS/Kestrel or API gateway

---

KNOWN_RISKS:
- NONE: No hardcoded secrets, credentials, or sensitive configuration found in WebAppComponents source
- NONE: CORS configuration handled by hosting web application, not in library

---

OBSERVABILITY:
- LOGGING: None directly implemented in WebAppComponents
- METRICS: None directly implemented in WebAppComponents
- HEALTH_CHECK: None implemented in library
NOTES: Logging and observability delegated to hosting application and remote Catalog.API

---

DEPLOYMENT:
DOCKERFILE: Included in parent WebApp or HybridApp containers
NOTES: WebAppComponents is compiled as part of hosting applications; not deployed independently. Distributed as NuGet package reference in WebApp.csproj
