# WebApp

## Overview
The WebApp is the main e-commerce frontend for the eShop application. It's a Blazor Server application that provides the user interface for browsing products, managing the shopping cart, and placing orders.

## Technology Stack
- **Framework**: Blazor Server (.NET 10)
- **UI Components**: WebAppComponents (shared library)
- **Authentication**: OpenID Connect with Identity.API
- **State Management**: Server-side with SignalR

## Key Features
- Product catalog browsing
- Product search (including AI-powered semantic search)
- Shopping cart management
- User authentication and registration
- Order placement and history
- Real-time order status updates

## Key Components

### Pages
- `/` - Home page with featured products
- `/catalog` - Product catalog with filtering
- `/item/{id}` - Product detail page
- `/cart` - Shopping cart
- `/checkout` - Order checkout
- `/orders` - Order history
- `/order/{id}` - Order details

### Services
- `BasketService` - Shopping cart operations (calls Basket.API)
- `CatalogService` - Product catalog operations (calls Catalog.API)
- `OrderingService` - Order operations (calls Ordering.API)
- `OrderStatusNotificationService` - Real-time order updates

## Event Integration

### Subscribed Events
| Event | Handler | Action |
|-------|---------|--------|
| `OrderStatusChangedToAwaitingValidationIntegrationEvent` | Handler | Updates UI with order status |
| `OrderStatusChangedToPaidIntegrationEvent` | Handler | Updates UI with order status |
| `OrderStatusChangedToStockConfirmedIntegrationEvent` | Handler | Updates UI with order status |
| `OrderStatusChangedToShippedIntegrationEvent` | Handler | Updates UI with order status |
| `OrderStatusChangedToCancelledIntegrationEvent` | Handler | Updates UI with order status |
| `OrderStatusChangedToSubmittedIntegrationEvent` | Handler | Updates UI with order status |

### Published Events
None - WebApp is a consumer of events for real-time updates.

## Project Structure
```
WebApp/
├── Components/              # Blazor components
│   ├── Layout/              # Layout components
│   └── Pages/               # Page components
├── IntegrationEvents/       # Event handlers for real-time updates
│   └── EventHandling/       # Integration event handlers
├── Services/                # API client services
├── Extensions.cs            # Service configuration
└── Program.cs               # Application entry point
```

## API Clients
The WebApp communicates with backend services via HTTP:
- **Catalog.API** - Product information
- **Basket.API** - Shopping cart (gRPC or HTTP)
- **Ordering.API** - Order management
- **Identity.API** - Authentication

## Authentication Flow
1. User clicks login
2. Redirected to Identity.API
3. User authenticates
4. Redirected back with authorization code
5. WebApp exchanges code for tokens
6. User session established

## Real-Time Updates
- Order status changes are pushed via SignalR
- Integration events trigger UI updates
- No page refresh required for status changes

## Dependencies
- **WebAppComponents**: Shared Blazor components
- **Catalog.API**: Product catalog
- **Basket.API**: Shopping cart
- **Ordering.API**: Order management
- **Identity.API**: Authentication
- **RabbitMQ**: For receiving order status events

## Configuration
Key settings:
- API endpoint URLs (via Aspire service discovery)
- Authentication configuration
- RabbitMQ connection for events

## Running Locally
This service is orchestrated by `eShop.AppHost` via .NET Aspire. Run the AppHost project to start all services with proper configuration.

Access the application at: `https://localhost:port` (port assigned by Aspire)

## Notes
- Uses server-side rendering for SEO
- SignalR connection for real-time features
- Responsive design for mobile and desktop
