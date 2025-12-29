# Catalog.API

## Overview
The Catalog API is a microservice responsible for managing the product catalog in the eShop application. It provides product browsing, search (including AI-powered semantic search), and inventory management.

## Technology Stack
- **Framework**: ASP.NET Core 10
- **Database**: PostgreSQL with pgvector extension
- **ORM**: Entity Framework Core 10
- **AI Integration**: Azure OpenAI / Ollama for embeddings
- **Communication**: REST API

## Key Components

### API Endpoints
- `GET /api/v1/catalog/items` - Get paginated catalog items
- `GET /api/v1/catalog/items/{id}` - Get item by ID
- `GET /api/v1/catalog/items/by` - Get items by IDs
- `GET /api/v1/catalog/items/type/{typeId}/brand/{brandId}` - Filter by type and brand
- `GET /api/v1/catalog/types` - Get all catalog types
- `GET /api/v1/catalog/brands` - Get all brands
- `GET /api/v1/catalog/items/withsemanticrelevance/{text}` - AI-powered semantic search
- `PUT /api/v1/catalog/items` - Update item
- `POST /api/v1/catalog/items` - Create item
- `DELETE /api/v1/catalog/items/{id}` - Delete item

### AI/Semantic Search
- Uses pgvector for vector similarity search
- Generates embeddings via Azure OpenAI or Ollama
- Enables natural language product search

### Models
- `CatalogItem` - Product entity with name, description, price, stock
- `CatalogBrand` - Brand entity
- `CatalogType` - Product type/category entity

## Event Integration

### Subscribed Events
| Event | Handler | Action |
|-------|---------|--------|
| `OrderStatusChangedToAwaitingValidationIntegrationEvent` | `OrderStatusChangedToAwaitingValidationIntegrationEventHandler` | Validates stock availability for order items |
| `OrderStatusChangedToPaidIntegrationEvent` | `OrderStatusChangedToPaidIntegrationEventHandler` | Decrements stock for paid order items |

### Published Events
| Event | Trigger |
|-------|---------|
| `OrderStockConfirmedIntegrationEvent` | When stock is available for all order items |
| `OrderStockRejectedIntegrationEvent` | When stock is insufficient for any order item |
| `ProductPriceChangedIntegrationEvent` | When product price is updated |

## Dependencies
- **PostgreSQL**: Product catalog database with pgvector
- **Identity.API**: For authentication
- **RabbitMQ**: For event bus communication
- **Azure OpenAI/Ollama**: Optional, for semantic search

## Project Structure
```
Catalog.API/
├── Apis/                    # API endpoint definitions
├── Infrastructure/          # EF Core context, migrations
├── IntegrationEvents/       # Events and handlers
│   ├── Events/              # Event definitions
│   └── EventHandling/       # Event handlers
├── Model/                   # Domain models
├── Services/                # Business services
├── Extensions.cs            # Service configuration
└── Program.cs               # Application entry point
```

## Database Schema
- `Catalog` - Main product table
- `CatalogBrand` - Brands lookup table
- `CatalogType` - Product types lookup table
- `IntegrationEventLog` - Outbox pattern event log

## Configuration
Key settings:
- PostgreSQL connection (via Aspire)
- AI services configuration (optional)
- RabbitMQ connection for event bus

## Running Locally
This service is orchestrated by `eShop.AppHost` via .NET Aspire. Run the AppHost project to start all services with proper configuration.
