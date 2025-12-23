# Shipping.API - eShop Shipping Management Service

## Overview
ASP.NET Core Minimal API for managing shipments, routes, and shippers.

## Tech Stack
- **ASP.NET Core Minimal APIs** - HTTP endpoints
- **Entity Framework Core** - Data access with PostgreSQL
- **.NET Aspire** - Service defaults and orchestration
- **Clean Architecture** - Domain/Infrastructure/API separation
- **RabbitMQ** - Integration events

## Project Structure
```
Shipping.API/
├── Apis/                    # Minimal API endpoint definitions
│   ├── ShipmentApi.cs       # Shipment management endpoints
│   └── ShipperApi.cs        # Shipper management endpoints
├── Application/
│   └── IntegrationEvents/   # Event publishing and handling
├── Extensions/              # DI and service registration
├── Infrastructure/          # DB seeding, identity service
└── Program.cs               # Application entry point

Shipping.Domain/
├── AggregatesModel/
│   ├── ShipmentAggregate/   # Shipment, Waypoint, StatusHistory
│   └── ShipperAggregate/    # Shipper entity
├── Events/                  # Domain events
├── SeedWork/                # Base classes
└── Exceptions/              # Domain exceptions

Shipping.Infrastructure/
├── EntityConfigurations/    # EF Core type configurations
├── Repositories/            # Repository implementations
└── ShippingContext.cs       # DbContext
```

## Domain Model
- **Shipment** - Aggregate root with order info, status, waypoints
- **ShipmentWaypoint** - Route stops at warehouses
- **ShipmentStatusHistory** - Status change tracking
- **Shipper** - Delivery person entity

## Shipment Status Flow
```
Created → ShipperAssigned → PickedUpFromWarehouse → InTransitToWarehouse →
ArrivedAtWarehouse → DeliveringToCustomer → Delivered
                                          ↘ Cancelled → ReturnedToWarehouse
```

## API Endpoints
Base path: `/api/shipments`

### Shipment Management (Admin)
- `GET /` - List all shipments
- `GET /{id}` - Get shipment by ID
- `GET /order/{orderId}` - Get shipment by order
- `GET /status/{status}` - Filter by status
- `GET /{id}/route` - Get route waypoints
- `GET /{id}/history` - Get status history
- `POST /{id}/assign-shipper` - Assign shipper
- `POST /{id}/cancel` - Cancel shipment

Base path: `/api/shippers`

### Shipper Management (Admin)
- `GET /` - List all shippers
- `POST /` - Create shipper
- `PUT /{id}` - Update shipper
- `DELETE /{id}` - Delete shipper

### Shipper Self-Service
- `GET /me` - Current shipper profile
- `GET /me/assigned` - My assigned shipments
- `GET /me/available` - Available to claim
- `GET /me/history` - Completed shipments
- `POST /me/claim/{shipmentId}` - Claim shipment
- `PUT /me/shipment/{id}/pickup` - Pickup from warehouse
- `PUT /me/shipment/{id}/arrive/{waypointId}` - Arrive at waypoint
- `PUT /me/shipment/{id}/depart/{waypointId}` - Depart from waypoint
- `PUT /me/shipment/{id}/deliver` - Mark delivered

## Integration Events

### Subscribes To:
- `OrderStatusChangedToPaidIntegrationEvent` - Creates shipment with route
- `OrderCancelledIntegrationEvent` - Cancels shipment, triggers inventory restore

### Publishes:
- `ShipmentCompletedIntegrationEvent` - Triggers order shipped status
- `ShipmentCancelledIntegrationEvent` - Triggers inventory restore

## Database
- Connection: `shippingdb` (PostgreSQL via Aspire)
- Schema: `shipping`
- Tables: `Shipments`, `ShipmentWaypoints`, `ShipmentStatusHistory`, `Shippers`

## Authentication
Requires JWT authentication via Identity.API. All endpoints require authorization.

## Running Locally
```powershell
# Via Aspire host (recommended)
dotnet run --project src/eShop.AppHost

# Create migrations
cd src/Shipping.Infrastructure
dotnet ef migrations add <MigrationName> -s ../Shipping.API
```
